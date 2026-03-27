(function () {
  var purchaseBtn = document.getElementById('purchaseTicketBtn');
  var purchaseStatusEl = document.getElementById('purchaseStatus');
  var timelineStatusEl = document.getElementById('timelineStatus');

  var lotteryTabBtn = document.getElementById('lotteryTabBtn');
  var accountTabBtn = document.getElementById('accountTabBtn');
  var lotteryTabPanel = document.getElementById('lotteryTabPanel');
  var accountTabPanel = document.getElementById('accountTabPanel');

  var currentDrawStateBadgeEl = document.getElementById('currentDrawStateBadge');
  var currentDrawEmptyEl = document.getElementById('currentDrawEmpty');
  var currentDrawContentEl = document.getElementById('currentDrawContent');
  var currentDrawIdEl = document.getElementById('currentDrawId');
  var currentDrawPrizePoolEl = document.getElementById('currentDrawPrizePool');
  var currentDrawCreatedAtEl = document.getElementById('currentDrawCreatedAt');
  var currentDrawNumbersWrapEl = document.getElementById('currentDrawNumbersWrap');
  var currentDrawNumbersEl = document.getElementById('currentDrawNumbers');

  var currentTicketsEmptyEl = document.getElementById('currentTicketsEmpty');
  var currentTicketsListEl = document.getElementById('currentTicketsList');

  var openHistoryBtn = document.getElementById('openHistoryBtn');
  var historySheetEl = document.getElementById('historySheet');
  var historyBackdropEl = document.getElementById('historyBackdrop');
  var closeHistoryBtn = document.getElementById('closeHistoryBtn');
  var historyEmptyEl = document.getElementById('historyEmpty');
  var historyListEl = document.getElementById('historyList');

  var highlightTicketId = null;
  var lastStateSig = null;
  var latestState = { currentDraw: null, currentTickets: [], history: [] };
  var initData = null;
  var devTelegramUserId = null;

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

  function formatPrizePool(value) {
    var amount = Number(value || 0);
    if (!Number.isFinite(amount)) amount = 0;
    return amount.toFixed(2);
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

    parseNumbers(numbersStr).forEach(function (n) {
      var ball = document.createElement('div');
      ball.className = 'ball';
      ball.textContent = String(n);
      numbersWrap.appendChild(ball);
    });

    return numbersWrap;
  }

  function createTicketEl(ticket) {
    var el = document.createElement('div');
    el.className = 'ticket';

    if (highlightTicketId && ticket && ticket.id === highlightTicketId) {
      el.classList.add('enter');
      el.classList.add('highlight');
      highlightTicketId = null;

      setTimeout(function () {
        try { el.classList.remove('highlight'); } catch (e) { }
      }, 1500);
    }

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

  function createHistoryGroupEl(group) {
    var container = document.createElement('div');
    container.className = 'draw-group';

    var draw = group.draw || null;

    var header = document.createElement('div');
    header.className = 'draw-header';

    var top = document.createElement('div');
    top.className = 'draw-header-top';

    var title = document.createElement('div');
    title.className = 'draw-header-title';
    title.textContent = 'Draw #' + group.drawId;

    var meta = document.createElement('div');
    meta.className = 'draw-header-meta';
    meta.textContent = draw ? formatUtc(draw.createdAtUtc) : 'Created before current format';

    top.appendChild(title);
    top.appendChild(meta);
    header.appendChild(top);

    if (draw) {
      var info = document.createElement('div');
      info.className = 'draw-header-label';
      info.textContent = 'State: ' + draw.state + ' • Prize pool: ' + formatPrizePool(draw.prizePool);
      header.appendChild(info);

      if (draw.numbers) {
        header.appendChild(createNumbersRow(draw.numbers));
      }
    }

    container.appendChild(header);

    var tickets = group.tickets || [];
    if (tickets.length === 0) {
      var empty = document.createElement('div');
      empty.className = 'muted history-empty-row';
      empty.textContent = 'No tickets for this draw.';
      container.appendChild(empty);
    } else {
      var ticketsWrap = document.createElement('div');
      ticketsWrap.className = 'draw-group-tickets';
      tickets.forEach(function (ticket) {
        ticketsWrap.appendChild(createTicketEl(ticket));
      });
      container.appendChild(ticketsWrap);
    }

    return container;
  }

  function renderCurrentDraw(draw) {
    if (!currentDrawEmptyEl || !currentDrawContentEl || !currentDrawStateBadgeEl) return;

    if (!draw) {
      currentDrawStateBadgeEl.textContent = 'waiting';
      currentDrawStateBadgeEl.className = 'state-badge state-badge-muted';
      currentDrawEmptyEl.hidden = false;
      currentDrawContentEl.hidden = true;
      if (purchaseBtn) {
        purchaseBtn.disabled = true;
        purchaseBtn.title = 'No active draw available.';
      }
      return;
    }

    currentDrawStateBadgeEl.textContent = draw.state;
    currentDrawStateBadgeEl.className = 'state-badge ' + (draw.state === 'active' ? 'state-badge-active' : draw.state === 'finished' ? 'state-badge-finished' : 'state-badge-upcoming');
    currentDrawEmptyEl.hidden = true;
    currentDrawContentEl.hidden = false;

    if (currentDrawIdEl) currentDrawIdEl.textContent = '#' + draw.id;
    if (currentDrawPrizePoolEl) currentDrawPrizePoolEl.textContent = formatPrizePool(draw.prizePool);
    if (currentDrawCreatedAtEl) currentDrawCreatedAtEl.textContent = formatUtc(draw.createdAtUtc);

    var hasNumbers = !!(draw.numbers && String(draw.numbers).length > 0);
    if (currentDrawNumbersWrapEl) currentDrawNumbersWrapEl.hidden = !hasNumbers;
    if (currentDrawNumbersEl) {
      currentDrawNumbersEl.innerHTML = '';
      if (hasNumbers) currentDrawNumbersEl.appendChild(createNumbersRow(draw.numbers));
    }

    if (purchaseBtn) {
      purchaseBtn.disabled = draw.state !== 'active';
      purchaseBtn.title = draw.state === 'active' ? '' : 'Only the active draw accepts purchases.';
    }
  }

  function renderCurrentTickets(tickets) {
    if (!currentTicketsListEl || !currentTicketsEmptyEl) return;

    currentTicketsListEl.innerHTML = '';

    var list = tickets || [];
    currentTicketsEmptyEl.hidden = list.length > 0;

    list.forEach(function (ticket) {
      currentTicketsListEl.appendChild(createTicketEl(ticket));
    });
  }

  function renderHistory(groups) {
    if (!historyListEl || !historyEmptyEl) return;

    historyListEl.innerHTML = '';

    var list = groups || [];
    historyEmptyEl.hidden = list.length > 0;

    list.forEach(function (group) {
      historyListEl.appendChild(createHistoryGroupEl(group));
    });
  }

  function setActiveTab(name) {
    var isLottery = name !== 'account';

    if (lotteryTabPanel) lotteryTabPanel.hidden = !isLottery;
    if (accountTabPanel) accountTabPanel.hidden = isLottery;

    if (lotteryTabBtn) lotteryTabBtn.classList.toggle('tabbar-btn-active', isLottery);
    if (accountTabBtn) accountTabBtn.classList.toggle('tabbar-btn-active', !isLottery);
  }

  function openHistory() {
    if (!historySheetEl) return;
    historySheetEl.hidden = false;
    document.body.classList.add('sheet-open');
  }

  function closeHistory() {
    if (!historySheetEl) return;
    historySheetEl.hidden = true;
    document.body.classList.remove('sheet-open');
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
      if (!r.ok) {
        return r.json().catch(function () { return null; }).then(function (body) {
          var msg = (body && body.error) ? body.error : ('HTTP ' + r.status);
          throw new Error(msg);
        });
      }

      return r.json();
    });
  }

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

  function computeStateSig(state) {
    try {
      return JSON.stringify({
        currentDraw: state && state.currentDraw ? {
          id: state.currentDraw.id,
          prizePool: state.currentDraw.prizePool,
          state: state.currentDraw.state,
          numbers: state.currentDraw.numbers || null,
          createdAtUtc: state.currentDraw.createdAtUtc || null
        } : null,
        currentTickets: (state && state.currentTickets || []).map(function (ticket) {
          return { id: ticket.id, drawId: ticket.drawId, numbers: ticket.numbers || null, purchasedAtUtc: ticket.purchasedAtUtc || null };
        }),
        history: (state && state.history || []).map(function (group) {
          return {
            drawId: group.drawId,
            draw: group.draw ? {
              id: group.draw.id,
              prizePool: group.draw.prizePool,
              state: group.draw.state,
              numbers: group.draw.numbers || null,
              createdAtUtc: group.draw.createdAtUtc || null
            } : null,
            tickets: (group.tickets || []).map(function (ticket) {
              return { id: ticket.id, numbers: ticket.numbers || null, purchasedAtUtc: ticket.purchasedAtUtc || null };
            })
          };
        })
      });
    } catch (e) {
      return String(Math.random());
    }
  }

  function applyState(state) {
    latestState = state || { currentDraw: null, currentTickets: [], history: [] };

    renderCurrentDraw(latestState.currentDraw || null);
    renderCurrentTickets(latestState.currentTickets || []);
    renderHistory(latestState.history || []);
  }

  function refreshState() {
    if (!initData && !devTelegramUserId) return;

    setTimelineStatus('loading...');

    return postJson('/api/timeline', { initData: initData || '' }, getDevHeadersIfNeeded())
      .then(function (res) {
        if (!(res && res.ok && res.state)) return;

        if (highlightTicketId) {
          var found = false;
          (res.state.currentTickets || []).forEach(function (ticket) {
            if (ticket && ticket.id === highlightTicketId) found = true;
          });
          if (!found) highlightTicketId = null;
        }

        var sig = computeStateSig(res.state);
        if (sig !== lastStateSig) {
          lastStateSig = sig;
          applyState(res.state);
        }

        setTimelineStatus('');
      })
      .catch(function (err) {
        console.warn('Failed to load app state', err);
        setTimelineStatus(err.message);
      });
  }

  function startPolling() {
    try {
      setInterval(function () {
        refreshState();
      }, 4000);
    } catch (e) {
    }
  }

  function purchaseTicket() {
    if (!initData && !devTelegramUserId) return;
    if (!latestState.currentDraw || latestState.currentDraw.state !== 'active') {
      setPurchaseStatus('There is no active draw right now.');
      return;
    }

    if (purchaseBtn) purchaseBtn.disabled = true;
    setPurchaseStatus('Buying ticket...');

    postJson('/api/tickets/purchase', { initData: initData || '' }, getDevHeadersIfNeeded())
      .then(function (res) {
        if (res && res.ok && res.ticket) {
          highlightTicketId = res.ticket.id;
          setPurchaseStatus('Ticket purchased.');
          return refreshState();
        }

        setPurchaseStatus('Purchase failed.');
      })
      .catch(function (err) {
        setPurchaseStatus(err.message);
      })
      .finally(function () {
        if (purchaseBtn) {
          purchaseBtn.disabled = !(latestState.currentDraw && latestState.currentDraw.state === 'active');
        }
      });
  }

  if (purchaseBtn) purchaseBtn.addEventListener('click', purchaseTicket);
  if (lotteryTabBtn) lotteryTabBtn.addEventListener('click', function () { setActiveTab('lottery'); });
  if (accountTabBtn) accountTabBtn.addEventListener('click', function () { setActiveTab('account'); });
  if (openHistoryBtn) openHistoryBtn.addEventListener('click', openHistory);
  if (closeHistoryBtn) closeHistoryBtn.addEventListener('click', closeHistory);
  if (historyBackdropEl) historyBackdropEl.addEventListener('click', closeHistory);

  setActiveTab('lottery');
  renderCurrentDraw(null);
  renderCurrentTickets([]);
  renderHistory([]);

  if (!window.Telegram || !Telegram.WebApp) {
    devTelegramUserId = getOrCreateDevTelegramUserId();
    refreshState();
    startPolling();
    return;
  }

  startPolling();

  try {
    Telegram.WebApp.setHeaderColor('#ee964b');
    Telegram.WebApp.setBackgroundColor('#ee964b');
  } catch (e) {
  }

  Telegram.WebApp.ready();
  Telegram.WebApp.expand();

  try {
    initData = Telegram.WebApp.initData;
    if (initData && initData.length > 0) {
      postJson('/api/auth/telegram', { initData: initData })
        .then(function () { return refreshState(); })
        .catch(function (err) {
          console.warn('Failed to auth with Telegram initData', err);
          setPurchaseStatus(err.message);
        });
    }
  } catch (e) {
    console.warn('Telegram auth initData error', e);
  }
})();
