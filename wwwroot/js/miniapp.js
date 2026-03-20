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

  function createDrawHeaderEl(drawId, draw) {
    var header = document.createElement('div');
    header.className = 'draw-header';

    var top = document.createElement('div');
    top.className = 'draw-header-top';

    var title = document.createElement('div');
    title.className = 'draw-header-title';
    title.textContent = 'Draw #' + drawId;

    var meta = document.createElement('div');
    meta.className = 'draw-header-meta';
    meta.textContent = draw ? formatUtc(draw.createdAtUtc) : 'upcoming';

    top.appendChild(title);
    top.appendChild(meta);

    header.appendChild(top);

    if (draw && draw.numbers) {
      var label = document.createElement('div');
      label.className = 'draw-header-label';
      label.textContent = 'Winning numbers';
      header.appendChild(label);
      header.appendChild(createNumbersRow(draw.numbers));
    } else {
      var label2 = document.createElement('div');
      label2.className = 'draw-header-label';
      label2.textContent = 'Tickets for the next draw';
      header.appendChild(label2);
    }

    return header;
  }

  function createTicketEl(ticket) {
    var el = document.createElement('div');
    el.className = 'ticket';

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

    el.appendChild(header);
    el.appendChild(createNumbersRow(ticket.numbers));

    return el;
  }

  function createGroupEl(group) {
    var li = document.createElement('li');
    li.className = 'timeline-group';

    var container = document.createElement('div');
    container.className = 'draw-group';

    container.appendChild(createDrawHeaderEl(group.drawId, group.draw));

    var tickets = group.tickets || [];
    if (tickets.length === 0) {
      var empty = document.createElement('div');
      empty.className = 'muted';
      empty.style.padding = '10px 2px 2px';
      empty.textContent = 'No tickets.';
      container.appendChild(empty);
    } else {
      var ticketsWrap = document.createElement('div');
      ticketsWrap.className = 'draw-group-tickets';
      tickets.forEach(function (t) { ticketsWrap.appendChild(createTicketEl(t)); });
      container.appendChild(ticketsWrap);
    }

    li.appendChild(container);
    return li;
  }

  function setGroups(groups) {
    if (!timelineListEl || !timelineEmptyEl) return;

    timelineListEl.innerHTML = '';

    var list = groups || [];
    if (list.length === 0) {
      timelineEmptyEl.style.display = '';
      return;
    }

    timelineEmptyEl.style.display = 'none';
    list.forEach(function (g) {
      timelineListEl.appendChild(createGroupEl(g));
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
          setGroups(res.groups);
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
