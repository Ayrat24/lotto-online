(function () {
  var infoEl = document.getElementById('info');
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

    // update count
    var current = ticketsListEl.children ? ticketsListEl.children.length : null;
    if (typeof current === 'number') setTicketsCount(current);
  }

  // Always try to load backend-controlled text
  fetch('/api/text')
    .then(function (r) { return r.ok ? r.json() : Promise.reject(new Error('HTTP ' + r.status)); })
    .then(function (data) {
      if (data && typeof data.text === 'string') {
        infoEl.textContent = data.text;
      }
    })
    .catch(function (err) {
      console.warn('Failed to load /api/text', err);
    });

  function postJson(url, payload) {
    return fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
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

  function refreshTickets() {
    if (!initData) return;
    return postJson('/api/tickets/list', { initData: initData })
      .then(function (res) {
        if (res && res.ok) setTickets(res.tickets);
      })
      .catch(function (err) {
        console.warn('Failed to list tickets', err);
      });
  }

  function purchaseTicket() {
    if (!initData) {
      setPurchaseStatus('Open this page inside Telegram to purchase tickets.');
      return;
    }

    purchaseBtn.disabled = true;
    setPurchaseStatus('Purchasing...');

    postJson('/api/tickets/purchase', { initData: initData })
      .then(function (res) {
        if (res && res.ok && res.ticket) {
          setPurchaseStatus('Ticket purchased');
          prependTicket(res.ticket);
        } else {
          setPurchaseStatus('Purchase failed.');
        }
      })
      .catch(function (err) {
        setPurchaseStatus('Purchase failed: ' + err.message);
      })
      .finally(function () {
        purchaseBtn.disabled = false;
      });
  }

  if (purchaseBtn) purchaseBtn.addEventListener('click', purchaseTicket);

  if (!window.Telegram || !Telegram.WebApp) {
    setTickets([]);
    setPurchaseStatus('Tip: open inside Telegram to authenticate and purchase tickets.');
    return;
  }

  Telegram.WebApp.ready();
  Telegram.WebApp.expand();

  try {
    initData = Telegram.WebApp.initData;
    if (initData && initData.length > 0) {
      postJson('/api/auth/telegram', { initData: initData })
        .then(function () { return refreshTickets(); })
        .catch(function (err) {
          console.warn('Failed to auth with Telegram initData', err);
          setPurchaseStatus('Auth failed: ' + err.message);
        });
    }
  } catch (e) {
    console.warn('Telegram auth initData error', e);
  }

  document.getElementById('closeBtn').addEventListener('click', function () {
    Telegram.WebApp.close();
  });
})();
