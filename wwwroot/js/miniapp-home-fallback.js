(function () {
  function getTelegramInitData() {
    try {
      return window.Telegram && window.Telegram.WebApp && window.Telegram.WebApp.initData
        ? String(window.Telegram.WebApp.initData)
        : '';
    } catch (e) {
      return '';
    }
  }

  function resolveInitData() {
    var params = new URLSearchParams(window.location.search || '');
    var forceLocalDebug = params.get('debug') === '1' || params.get('mode') === 'local-debug';
    var telegramInitData = getTelegramInitData();

    if (forceLocalDebug || !telegramInitData) {
      return { initData: 'local-debug', isLocalDebug: true };
    }

    return { initData: telegramInitData, isLocalDebug: false };
  }

  function postJson(url, payload) {
    return fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload || {})
    }).then(function (response) {
      return response.text().then(function (text) {
        var data = null;
        try { data = text ? JSON.parse(text) : null; } catch (e) { data = null; }
        if (!response.ok) {
          var message = data && (data.error || data.title || data.message)
            ? String(data.error || data.title || data.message)
            : 'Request failed: ' + response.status;
          throw new Error(message);
        }
        return data;
      });
    });
  }

  function formatCurrency(value, locale, digits) {
    var amount = Number(value || 0);
    if (!Number.isFinite(amount)) amount = 0;
    return '$' + amount.toLocaleString(locale || 'en-US', {
      minimumFractionDigits: digits == null ? 2 : digits,
      maximumFractionDigits: digits == null ? 2 : digits
    });
  }

  function formatJackpot(value, locale) {
    var amount = Number(value || 0);
    if (!Number.isFinite(amount)) amount = 0;
    return '$' + Math.round(amount).toLocaleString(locale || 'en-US');
  }

  function formatCountdown(targetUtc) {
    var targetMs = Date.parse(targetUtc || '');
    if (!Number.isFinite(targetMs)) return 'Schedule pending';

    var remaining = Math.max(0, Math.floor((targetMs - Date.now()) / 1000));
    var hours = Math.floor(remaining / 3600);
    var minutes = Math.floor((remaining % 3600) / 60);
    var seconds = remaining % 60;

    if (hours > 0) {
      return String(hours).padStart(2, '0') + ':' + String(minutes).padStart(2, '0') + ':' + String(seconds).padStart(2, '0');
    }

    return String(minutes).padStart(2, '0') + ':' + String(seconds).padStart(2, '0');
  }

  function compareDraws(mode) {
    return function (a, b) {
      var aClose = Date.parse((a && a.purchaseClosesAtUtc) || '') || Number.MAX_SAFE_INTEGER;
      var bClose = Date.parse((b && b.purchaseClosesAtUtc) || '') || Number.MAX_SAFE_INTEGER;
      var aJackpot = Number((a && a.prizePoolMatch5) || 0);
      var bJackpot = Number((b && b.prizePoolMatch5) || 0);
      var aCost = Number((a && a.ticketCost) || 0);
      var bCost = Number((b && b.ticketCost) || 0);
      var aId = Number((a && a.id) || 0);
      var bId = Number((b && b.id) || 0);

      if (mode === 'jackpot') return (bJackpot - aJackpot) || (aClose - bClose) || (aCost - bCost) || (bId - aId);
      if (mode === 'cheap') return (aCost - bCost) || (bJackpot - aJackpot) || (aClose - bClose) || (bId - aId);
      return (aClose - bClose) || (bJackpot - aJackpot) || (aCost - bCost) || (bId - aId);
    };
  }

  var runtime = resolveInitData();
  var state = {
    initData: runtime.initData,
    isLocalDebug: runtime.isLocalDebug,
    locale: 'en',
    user: { firstName: 'Player', lastName: '', username: '', balance: 0 },
    timeline: null,
    banners: [],
    loading: true,
    error: '',
    sortMode: 'closest'
  };

  function getIntlLocale() {
    if (state.locale === 'ru') return 'ru-RU';
    if (state.locale === 'uz') return 'uz-UZ';
    return 'en-US';
  }

  function getUserName() {
    var name = [state.user.firstName, state.user.lastName].filter(Boolean).join(' ').trim();
    return name || state.user.username || 'Player';
  }

  function getActiveDraws() {
    var draws = state.timeline && Array.isArray(state.timeline.activeDraws) ? state.timeline.activeDraws.slice() : [];
    return draws.filter(function (draw) {
      return draw && draw.state === 'active' && draw.canPurchase !== false;
    }).sort(compareDraws(state.sortMode));
  }

  function render() {
    var root = document.getElementById('app');
    if (!root) return;

    var featuredBanner = state.banners[0] || null;
    var activeDraws = getActiveDraws();
    var locale = getIntlLocale();

    var bannerHtml = featuredBanner
      ? '<section class="news-section"><div class="section-title">Latest news</div><article class="news-banner">'
        + '<img class="news-banner-image" src="' + String(featuredBanner.imageUrl || '') + '" alt="News banner"></article></section>'
      : '';

    var statusHtml = '';
    if (state.loading) {
      statusHtml = '<div class="status-card">Loading home screen…</div>';
    } else if (state.error) {
      statusHtml = '<div class="status-card status-card-error"><div>' + state.error + '</div><button class="retry-btn" id="fallbackRetryBtn">Retry</button></div>';
    } else if (!activeDraws.length) {
      statusHtml = '<div class="status-card">There are no active draws right now.</div>';
    }

    var drawCardsHtml = activeDraws.map(function (draw) {
      var color = String((draw && draw.cardColor) || 'gold').toLowerCase();
      return '<article class="draw-card draw-card-' + color + '">'
        + '<div class="draw-card-top"><div class="draw-title">Draw #' + draw.id + '</div><div class="draw-timer">' + formatCountdown(draw.purchaseClosesAtUtc) + '</div></div>'
        + '<div class="draw-jackpot-label">Jackpot</div>'
        + '<div class="draw-jackpot-value">' + formatJackpot(draw.prizePoolMatch5, locale) + '</div>'
        + '<div class="draw-footer"><div><div class="draw-cost-label">Ticket cost</div><div class="draw-cost-value">' + formatCurrency(draw.ticketCost, locale) + '</div></div>'
        + '<div class="draw-badges">'
        + '<span class="draw-badge">3: ' + formatCurrency(draw.prizePoolMatch3, locale, 0) + '</span>'
        + '<span class="draw-badge">4: ' + formatCurrency(draw.prizePoolMatch4, locale, 0) + '</span>'
        + '<span class="draw-badge">5: ' + formatCurrency(draw.prizePoolMatch5, locale, 0) + '</span>'
        + '</div></div></article>';
    }).join('');

    root.innerHTML = ''
      + '<div class="home-page">'
      + '  <header class="topbar">'
      + '    <div class="brand-block">'
      + '      <div class="avatar">' + getUserName().slice(0, 1).toUpperCase() + '</div>'
      + '      <div><div class="brand-name">' + getUserName() + '</div>' + (state.isLocalDebug ? '<div class="brand-meta">Local debug mode</div>' : '') + '</div>'
      + '    </div>'
      + '    <div class="balance-card"><div class="balance-label">Balance</div><div class="balance-value">' + formatCurrency(state.user.balance, locale) + '</div></div>'
      + '  </header>'
      + '  <main class="home-content">'
      +       bannerHtml
      + '    <section class="sort-tabs">'
      + '      <button class="sort-tab' + (state.sortMode === 'closest' ? ' active' : '') + '" data-sort="closest">Closest draw</button>'
      + '      <button class="sort-tab' + (state.sortMode === 'jackpot' ? ' active' : '') + '" data-sort="jackpot">Biggest jackpot</button>'
      + '      <button class="sort-tab' + (state.sortMode === 'cheap' ? ' active' : '') + '" data-sort="cheap">Cheapest tickets</button>'
      + '    </section>'
      + '    <section class="draws-section"><div class="section-title">Draw cards</div>'
      +         statusHtml
      +         (!state.loading && !state.error && activeDraws.length ? '<div class="draw-list">' + drawCardsHtml + '</div>' : '')
      + '    </section>'
      + '  </main>'
      + '</div>';

    var retryBtn = document.getElementById('fallbackRetryBtn');
    if (retryBtn) retryBtn.addEventListener('click', loadAll);

    var sortButtons = root.querySelectorAll('[data-sort]');
    for (var i = 0; i < sortButtons.length; i++) {
      sortButtons[i].addEventListener('click', function () {
        state.sortMode = this.getAttribute('data-sort') || 'closest';
        render();
      });
    }
  }

  function loadLocale() {
    return postJson('/api/localization/bootstrap', { initData: state.initData, locale: state.locale })
      .then(function (res) {
        if (res && res.ok) state.locale = String(res.locale || 'en');
      });
  }

  function loadAuth() {
    return postJson('/api/auth/telegram', { initData: state.initData })
      .then(function (res) {
        if (res && res.ok) {
          state.user.firstName = res.firstName || '';
          state.user.lastName = res.lastName || '';
          state.user.username = res.username || '';
          state.user.balance = Number(res.balance || 0);
        }
      });
  }

  function loadTimeline() {
    return postJson('/api/timeline', { initData: state.initData })
      .then(function (res) {
        if (res && res.ok) {
          state.timeline = res.state;
          state.user.balance = Number((res.state && res.state.balance) || state.user.balance || 0);
        }
      });
  }

  function loadBanners() {
    return postJson('/api/news-banners', { initData: state.initData, locale: state.locale })
      .then(function (res) {
        state.banners = res && res.ok && Array.isArray(res.banners) ? res.banners : [];
      });
  }

  function loadAll() {
    state.error = '';
    state.loading = true;
    render();

    return loadLocale()
      .then(loadAuth)
      .then(function () { return Promise.all([loadTimeline(), loadBanners()]); })
      .catch(function (error) {
        state.error = error && error.message ? error.message : 'Failed to load home screen.';
      })
      .finally(function () {
        state.loading = false;
        render();
      });
  }

  try {
    if (window.Telegram && window.Telegram.WebApp) {
      window.Telegram.WebApp.ready();
      window.Telegram.WebApp.expand();
    }
  } catch (e) {
  }

  render();
  loadAll();
  window.setInterval(function () {
    render();
  }, 1000);
  window.setInterval(function () {
    loadTimeline().then(render).catch(function () {});
    loadBanners().then(render).catch(function () {});
  }, 4000);
})();