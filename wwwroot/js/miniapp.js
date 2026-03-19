(function () {
  var infoEl = document.getElementById('info');
  var purchaseBtn = document.getElementById('purchaseTicketBtn');
  var purchaseStatusEl = document.getElementById('purchaseStatus');
  var ticketsListEl = document.getElementById('ticketsList');
  var ticketsEmptyEl = document.getElementById('ticketsEmpty');

  function setPurchaseStatus(text) {
    if (purchaseStatusEl) purchaseStatusEl.textContent = text || '';
  }

  function setTickets(tickets) {
    if (!ticketsListEl || !ticketsEmptyEl) return;

    ticketsListEl.innerHTML = '';
    if (!tickets || tickets.length === 0) {
      ticketsEmptyEl.style.display = '';
      return;
    }

    ticketsEmptyEl.style.display = 'none';
    tickets.forEach(function (t) {
      var li = document.createElement('li');
      li.textContent = (t.numbers || '') + '  (' + new Date(t.purchasedAtUtc).toISOString().replace('T', ' ').replace('Z', ' UTC') + ')';
      ticketsListEl.appendChild(li);
    });
  }

  // Always try to load backend-controlled text (works both in normal browser and Telegram)
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
          setPurchaseStatus('Ticket purchased: ' + res.ticket.numbers);
        } else {
          setPurchaseStatus('Purchase failed.');
        }
        return refreshTickets();
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
    // Not inside Telegram; keep the page usable in a normal browser.
    setTickets([]);
    setPurchaseStatus('Tip: open inside Telegram to authenticate and purchase tickets.');
    return;
  }

  Telegram.WebApp.ready();
  Telegram.WebApp.expand();

  // Secure login: send initData to backend for validation and user upsert.
  try {
    initData = Telegram.WebApp.initData;
    if (initData && initData.length > 0) {
      postJson('/api/auth/telegram', { initData: initData })
        .then(function (res) {
          if (res && res.ok && res.telegramUserId) {
            console.log('Authenticated Telegram user:', res.telegramUserId);
          }
          return refreshTickets();
        })
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
