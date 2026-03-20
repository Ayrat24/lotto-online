(function () {
  var purchaseBtn = document.getElementById('purchaseTicketBtn');
  var purchaseStatusEl = document.getElementById('purchaseStatus');

  var timelineListEl = document.getElementById('timelineList');
  var timelineEmptyEl = document.getElementById('timelineEmpty');
  var timelineStatusEl = document.getElementById('timelineStatus');

  function setPurchaseStatus(text) {
    if (purchaseStatusEl) purchaseStatusEl.textContent = text || '';
  }

  function setTimelineStatus(text) {
    if (timelineStatusEl) timelineStatusEl.textContent = text || '';
  }

  function formatUtc(iso) {
    try {
      return new Date(iso).toISOString().replace('T', ' ').replace('Z', ' UTC');
    } catch (e) {
      return String(iso || '');
    }
  }

  function parseNumbers(numbersStr) {
    if (!numbersStr) return [];
    return String(numbersStr)
      .split(',')
      .map(function (x) { return parseInt(x, 10); })
      .filter(function (n) { return Number.isFinite(n); });
  }

  function createNumbersRow(numbersStr) {
    var numbersWrap = document.createElement('div');
    numbersWrap.className = 'ticket-numbers';

    var nums = parseNumbers(numbersStr);
    nums.forEach(function (n) {
      var ball = document.createElement('div');
      ball.className = 'ball';
      ball.textContent = String(n);
      numbersWrap.appendChild(ball);
    });

    return numbersWrap;
  }

  function createDrawEl(draw) {
    var li = document.createElement('li');
    li.className = 'timeline-item draw-item';

    var card = document.createElement('div');
    card.className = 'ticket draw';

    var header = document.createElement('div');
    header.className = 'ticket-header';

    var title = document.createElement('div');
    title.className = 'ticket-title';
    title.textContent = 'Draw #' + draw.id;

    var meta = document.createElement('div');
    meta.className = 'ticket-meta';
    meta.textContent = formatUtc(draw.createdAtUtc);

    header.appendChild(title);
    header.appendChild(meta);

    card.appendChild(header);

    var label = document.createElement('div');
    label.className = 'muted';
    label.style.marginBottom = '10px';
    label.textContent = 'Winning numbers';

    card.appendChild(label);
    card.appendChild(createNumbersRow(draw.numbers));

    li.appendChild(card);
    return li;
  }

  function createTicketEl(ticket, withEnterAnimation) {
    var li = document.createElement('li');
    li.className = 'timeline-item ticket-item' + (withEnterAnimation ? ' enter' : '');

    var wrap = document.createElement('div');
    wrap.className = 'ticket';

    var header = document.createElement('div');
    header.className = 'ticket-header';

    var title = document.createElement('div');
    title.className = 'ticket-title';
    title.textContent = 'Ticket #' + ticket.id;

    var meta = document.createElement('div');
    meta.className = 'ticket-meta';
    meta.textContent = formatUtc(ticket.purchasedAtUtc);

    header.appendChild(title);
    header.appendChild(meta);

    wrap.appendChild(header);
    wrap.appendChild(createNumbersRow(ticket.numbers));

    li.appendChild(wrap);
    return li;
  }

  function setTimeline(items) {
    if (!timelineListEl || !timelineEmptyEl) return;

    timelineListEl.innerHTML = '';

    var list = items || [];
    if (list.length === 0) {
      timelineEmptyEl.style.display = '';
      return;
    }

    timelineEmptyEl.style.display = 'none';
    list.forEach(function (it) {
      if (!it) return;
      if (it.type === 'draw' && it.draw) timelineListEl.appendChild(createDrawEl(it.draw));
      else if (it.type === 'ticket' && it.ticket) timelineListEl.appendChild(createTicketEl(it.ticket, false));
    });
  }

  function postJson(url, payload, extraHeaders) {
    var headers = { 'Content-Type': 'application/json' };
    if (extraHeaders) {
      Object.keys(extraHeaders).forEach(function (k) { headers[k] = extraHeaders[k]; });
    }

    return fetch(url, {
      method: 'POST',
      headers: headers,
      body: JSON.stringify(payload)
    }).then(function (r) {
      if (!r.ok) return r.json().catch(function () { return null; }).then(function (b) {
        var msg = (b && b.error) ? b.error : ('HTTP ' + r.status);
        throw new Error(msg);
      });
      return r.json();
    });
  }

  var initData = null;
  var devTelegramUserId = null;

  function getOrCreateDevTelegramUserId() {
    try {
      var key = 'miniapp.devTelegramUserId';
      var existing = localStorage.getItem(key);
      if (existing && /^\d+$/.test(existing)) return existing;

      var n = Math.floor(100000000 + Math.random() * 900000000);
      localStorage.setItem(key, String(n));
      return String(n);
    } catch (e) {
      return String(Math.floor(100000000 + Math.random() * 900000000));
    }
  }

  function getDevHeadersIfNeeded() {
    if (initData) return null;
    if (!devTelegramUserId) return null;
    return { 'X-Dev-TelegramUserId': devTelegramUserId };
  }

  function refreshTimeline() {
    if (!initData && !devTelegramUserId) return;
    setTimelineStatus('loading...');
    return postJson('/api/timeline', { initData: initData || '' }, getDevHeadersIfNeeded())
      .then(function (res) {
        if (res && res.ok) {
          setTimeline(res.items);
          setTimelineStatus('');
        }
      })
      .catch(function (err) {
        console.warn('Failed to load timeline', err);
        setTimelineStatus(err.message);
      });
  }

  function purchaseTicket() {
    if (!initData && !devTelegramUserId) return;

    if (purchaseBtn) purchaseBtn.disabled = true;
    setPurchaseStatus('...');

    postJson('/api/tickets/purchase', { initData: initData || '' }, getDevHeadersIfNeeded())
      .then(function (res) {
        if (res && res.ok && res.ticket) {
          setPurchaseStatus('');
          // Refresh so the ticket appears under the correct draw header.
          return refreshTimeline();
        }

        setPurchaseStatus('Purchase failed.');
      })
      .catch(function (err) {
        setPurchaseStatus(err.message);
      })
      .finally(function () {
        if (purchaseBtn) purchaseBtn.disabled = false;
      });
  }

  if (purchaseBtn) purchaseBtn.addEventListener('click', purchaseTicket);

  if (!window.Telegram || !Telegram.WebApp) {
    devTelegramUserId = getOrCreateDevTelegramUserId();
    refreshTimeline();
    return;
  }

  try {
    Telegram.WebApp.setHeaderColor('#ee964b');
    Telegram.WebApp.setBackgroundColor('#ee964b');
  } catch (e) {
  }

  Telegram.WebApp.ready();
  Telegram.WebApp.expand();

  try {
    Telegram.WebApp.setHeaderColor('#ee964b');
    Telegram.WebApp.setBackgroundColor('#ee964b');
  } catch (e) {
  }

  try {
    initData = Telegram.WebApp.initData;
    if (initData && initData.length > 0) {
      postJson('/api/auth/telegram', { initData: initData })
        .then(function () { return refreshTimeline(); })
        .catch(function (err) {
          console.warn('Failed to auth with Telegram initData', err);
          setPurchaseStatus(err.message);
        });
    }
  } catch (e) {
    console.warn('Telegram auth initData error', e);
  }
})();
