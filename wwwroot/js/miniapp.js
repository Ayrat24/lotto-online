(function () {
  var purchaseBtn = document.getElementById('purchaseTicketBtn');
  var purchaseStatusEl = document.getElementById('purchaseStatus');
  var ticketsListEl = document.getElementById('ticketsList');
  var ticketsEmptyEl = document.getElementById('ticketsEmpty');
  var ticketsCountEl = document.getElementById('ticketsCount');

  function setPurchaseStatus(text) {
    if (purchaseStatusEl) purchaseStatusEl.textContent = text || '';
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

  function createTicketEl(ticket, withEnterAnimation) {
    var li = document.createElement('li');
    li.className = 'ticket' + (withEnterAnimation ? ' enter' : '');

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

    var numbersWrap = document.createElement('div');
    numbersWrap.className = 'ticket-numbers';

    var nums = parseNumbers(ticket.numbers);
    nums.forEach(function (n) {
      var ball = document.createElement('div');
      ball.className = 'ball';
      ball.textContent = String(n);
      numbersWrap.appendChild(ball);
    });

    li.appendChild(header);
    li.appendChild(numbersWrap);

    return li;
  }

  function setTicketsCount(count) {
    if (!ticketsCountEl) return;
    ticketsCountEl.textContent = (typeof count === 'number') ? (count + ' total') : '';
  }

  function setTickets(tickets) {
    if (!ticketsListEl || !ticketsEmptyEl) return;

    ticketsListEl.innerHTML = '';

    var list = tickets || [];
    setTicketsCount(list.length);

    if (list.length === 0) {
      ticketsEmptyEl.style.display = '';
      return;
    }

    ticketsEmptyEl.style.display = 'none';
    list.forEach(function (t) {
      ticketsListEl.appendChild(createTicketEl(t, false));
    });
  }

  function prependTicket(ticket) {
    if (!ticketsListEl || !ticketsEmptyEl) return;

    ticketsEmptyEl.style.display = 'none';
    var el = createTicketEl(ticket, true);

    if (ticketsListEl.firstChild) ticketsListEl.insertBefore(el, ticketsListEl.firstChild);
    else ticketsListEl.appendChild(el);

    var current = ticketsListEl.children ? ticketsListEl.children.length : null;
    if (typeof current === 'number') setTicketsCount(current);
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

  function refreshTickets() {
    if (!initData && !devTelegramUserId) return;
    return postJson('/api/tickets/list', { initData: initData || '' }, getDevHeadersIfNeeded())
      .then(function (res) {
        if (res && res.ok) setTickets(res.tickets);
      })
      .catch(function (err) {
        console.warn('Failed to list tickets', err);
        setPurchaseStatus(err.message);
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
          prependTicket(res.ticket);
        } else {
          setPurchaseStatus('Purchase failed.');
        }
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
    refreshTickets();
    return;
  }

  // Ensure Telegram top panel/header uses our colors (Telegram may otherwise use its own theme/system defaults)
  try {
    // Use sandy-brown to match page background and card gradient
    Telegram.WebApp.setHeaderColor('#ee964b');
    Telegram.WebApp.setBackgroundColor('#ee964b');
  } catch (e) {
    // Some clients/versions may not support these API calls.
  }

  Telegram.WebApp.ready();
  Telegram.WebApp.expand();

  // Repeat color set after ready() for clients that require it
  try {
    Telegram.WebApp.setHeaderColor('#ee964b');
    Telegram.WebApp.setBackgroundColor('#ee964b');
  } catch (e) {
    // ignore
  }

  try {
    initData = Telegram.WebApp.initData;
    if (initData && initData.length > 0) {
      postJson('/api/auth/telegram', { initData: initData })
        .then(function () { return refreshTickets(); })
        .catch(function (err) {
          console.warn('Failed to auth with Telegram initData', err);
          setPurchaseStatus(err.message);
        });
    }
  } catch (e) {
    console.warn('Telegram auth initData error', e);
  }
})();
