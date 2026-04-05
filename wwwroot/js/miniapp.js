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
  var jackpotAmountEl = document.getElementById('jackpotAmount');
  var jackpotSubtitleEl = document.getElementById('jackpotSubtitle');
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

  var ticketPickerSheetEl = document.getElementById('ticketPickerSheet');
  var ticketPickerBackdropEl = document.getElementById('ticketPickerBackdrop');
  var closeTicketPickerBtn = document.getElementById('closeTicketPickerBtn');
  var ticketPickerGridEl = document.getElementById('ticketPickerGrid');
  var ticketPickerStatusEl = document.getElementById('ticketPickerStatus');
  var confirmTicketNumbersBtn = document.getElementById('confirmTicketNumbersBtn');

  var highlightTicketId = null;
  var lastStateSig = null;
  var latestState = { currentDraw: null, currentTickets: [], history: [] };
  var initData = null;
  var pickerNumbers = [1, 2, 3, 4, 5];

  var LOTTO_NUMBERS_COUNT = 5;
  var LOTTO_MIN = 1;
  var LOTTO_MAX = 36;

  function setPurchaseStatus(text) {
    if (purchaseStatusEl) purchaseStatusEl.textContent = text || '';
  }

  function setTimelineStatus(text) {
    if (timelineStatusEl) timelineStatusEl.textContent = text || '';
  }

  function formatUtc(iso) {
    try {
      return new Date(iso).toLocaleString();
    } catch (e) {
      return String(iso || '');
    }
  }

  function formatPrizePool(value) {
    var amount = Number(value || 0);
    if (!Number.isFinite(amount)) amount = 0;
    return amount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  function formatJackpot(value) {
    var amount = Number(value || 0);
    if (!Number.isFinite(amount)) amount = 0;
    return '$' + Math.round(amount).toLocaleString();
  }

  function parseNumbers(numbersStr) {
    if (!numbersStr) return [];
    return String(numbersStr)
      .split(',')
      .map(function (x) { return parseInt(x, 10); })
      .filter(function (n) { return Number.isFinite(n); });
  }

  function setTicketPickerStatus(text) {
    if (ticketPickerStatusEl) ticketPickerStatusEl.textContent = text || '';
  }

  function formatPickerNumber(n) {
    return String(n).padStart(2, '0');
  }

  function wrapPickerNumber(n) {
    if (n < LOTTO_MIN) return LOTTO_MAX;
    if (n > LOTTO_MAX) return LOTTO_MIN;
    return n;
  }

  function normalizePickerNumbers() {
    if (!Array.isArray(pickerNumbers)) pickerNumbers = [];

    while (pickerNumbers.length < LOTTO_NUMBERS_COUNT) {
      pickerNumbers.push(LOTTO_MIN + pickerNumbers.length);
    }

    if (pickerNumbers.length > LOTTO_NUMBERS_COUNT) {
      pickerNumbers = pickerNumbers.slice(0, LOTTO_NUMBERS_COUNT);
    }

    pickerNumbers = pickerNumbers.map(function (n) {
      n = parseInt(n, 10);
      if (!Number.isFinite(n)) return LOTTO_MIN;
      return Math.min(LOTTO_MAX, Math.max(LOTTO_MIN, n));
    });
  }

  function setSheetOpenClass() {
    var hasOpenSheet = !!(
      (historySheetEl && !historySheetEl.hidden) ||
      (ticketPickerSheetEl && !ticketPickerSheetEl.hidden)
    );

    document.body.classList.toggle('sheet-open', hasOpenSheet);
  }

  function validatePickerSelection() {
    var seen = {};
    var duplicates = false;

    for (var i = 0; i < pickerNumbers.length; i++) {
      var n = pickerNumbers[i];
      if (seen[n]) {
        duplicates = true;
        break;
      }
      seen[n] = true;
    }

    if (duplicates) return { ok: false, message: 'Please choose 5 unique numbers.' };
    return { ok: true, message: '' };
  }

  function updatePickerUi() {
    if (!ticketPickerGridEl) return;

    var values = ticketPickerGridEl.querySelectorAll('[data-picker-value]');
    for (var i = 0; i < values.length; i++) {
      var idx = parseInt(values[i].getAttribute('data-picker-value'), 10);
      if (!Number.isFinite(idx) || idx < 0 || idx >= pickerNumbers.length) continue;
      values[i].textContent = formatPickerNumber(pickerNumbers[idx]);
    }

    var validation = validatePickerSelection();
    setTicketPickerStatus(validation.message);
    if (confirmTicketNumbersBtn) confirmTicketNumbersBtn.disabled = !validation.ok;
  }

  function stepPickerIndex(index, delta) {
    if (!Number.isFinite(index) || index < 0 || index >= pickerNumbers.length) return;
    pickerNumbers[index] = wrapPickerNumber(pickerNumbers[index] + delta);
    updatePickerUi();
  }

  function attachPickerSwipeHandlers(target, index) {
    if (!target) return;

    var startY = null;

    function begin(y) {
      startY = y;
    }

    function end(y) {
      if (startY === null) return;
      var delta = y - startY;
      startY = null;

      if (Math.abs(delta) < 14) return;
      if (delta < 0) stepPickerIndex(index, 1);
      else stepPickerIndex(index, -1);
    }

    target.addEventListener('wheel', function (e) {
      e.preventDefault();
      stepPickerIndex(index, e.deltaY > 0 ? -1 : 1);
    }, { passive: false });

    target.addEventListener('mousedown', function (e) { begin(e.clientY); });
    target.addEventListener('mouseup', function (e) { end(e.clientY); });
    target.addEventListener('mouseleave', function () { startY = null; });

    target.addEventListener('touchstart', function (e) {
      if (!e.touches || e.touches.length === 0) return;
      begin(e.touches[0].clientY);
    }, { passive: true });

    target.addEventListener('touchend', function (e) {
      if (!e.changedTouches || e.changedTouches.length === 0) return;
      end(e.changedTouches[0].clientY);
    }, { passive: true });
  }

  function buildTicketPickerSlots() {
    if (!ticketPickerGridEl) return;

    normalizePickerNumbers();
    ticketPickerGridEl.innerHTML = '';

    for (var i = 0; i < LOTTO_NUMBERS_COUNT; i++) {
      var slot = document.createElement('div');
      slot.className = 'picker-slot';

      var upBtn = document.createElement('button');
      upBtn.type = 'button';
      upBtn.className = 'picker-arrow';
      upBtn.textContent = '^';
      upBtn.addEventListener('click', (function (idx) {
        return function () { stepPickerIndex(idx, 1); };
      })(i));

      var value = document.createElement('div');
      value.className = 'picker-value';

      var label = document.createElement('span');
      label.className = 'picker-value-label';
      label.textContent = 'No. ' + (i + 1);

      var number = document.createElement('span');
      number.className = 'picker-value-number';
      number.setAttribute('data-picker-value', String(i));
      number.textContent = formatPickerNumber(pickerNumbers[i]);

      value.appendChild(label);
      value.appendChild(number);

      var downBtn = document.createElement('button');
      downBtn.type = 'button';
      downBtn.className = 'picker-arrow';
      downBtn.textContent = 'v';
      downBtn.addEventListener('click', (function (idx) {
        return function () { stepPickerIndex(idx, -1); };
      })(i));

      attachPickerSwipeHandlers(value, i);

      slot.appendChild(upBtn);
      slot.appendChild(value);
      slot.appendChild(downBtn);
      ticketPickerGridEl.appendChild(slot);
    }

    updatePickerUi();
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
      if (currentDrawIdEl) currentDrawIdEl.textContent = 'PowerBall Global';
      if (currentDrawPrizePoolEl) currentDrawPrizePoolEl.textContent = '$0.00';
      if (currentDrawCreatedAtEl) currentDrawCreatedAtEl.textContent = 'Ends in --:--:--';
      if (jackpotAmountEl) jackpotAmountEl.textContent = '$0';
      if (jackpotSubtitleEl) jackpotSubtitleEl.textContent = 'The next draw is coming soon. Get your tickets now.';
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

    if (currentDrawIdEl) currentDrawIdEl.textContent = 'PowerBall Global • #' + draw.id;
    if (currentDrawPrizePoolEl) currentDrawPrizePoolEl.textContent = '$' + formatPrizePool(draw.prizePool);
    if (currentDrawCreatedAtEl) currentDrawCreatedAtEl.textContent = 'Opened ' + formatUtc(draw.createdAtUtc);
    if (jackpotAmountEl) jackpotAmountEl.textContent = formatJackpot(draw.prizePool);
    if (jackpotSubtitleEl) jackpotSubtitleEl.textContent = draw.state === 'active'
      ? 'The draw is live. Don\'t miss your chance to become a multi-millionaire!'
      : 'The next draw is coming soon. Get your tickets now.';

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
    setSheetOpenClass();
  }

  function closeHistory() {
    if (!historySheetEl) return;
    historySheetEl.hidden = true;
    setSheetOpenClass();
  }

  function openTicketPicker() {
    if (!ticketPickerSheetEl) return;

    buildTicketPickerSlots();
    ticketPickerSheetEl.hidden = false;
    setSheetOpenClass();
  }

  function closeTicketPicker() {
    if (!ticketPickerSheetEl) return;
    ticketPickerSheetEl.hidden = true;
    setSheetOpenClass();
  }

  function confirmSelectedTicketNumbers() {
    if (!initData) return;

    var validation = validatePickerSelection();
    if (!validation.ok) {
      setTicketPickerStatus(validation.message);
      return;
    }

    if (confirmTicketNumbersBtn) confirmTicketNumbersBtn.disabled = true;
    if (purchaseBtn) purchaseBtn.disabled = true;
    setTicketPickerStatus('Submitting ticket...');

    postJson('/api/tickets/purchase', {
      initData: initData || '',
      numbers: pickerNumbers.slice(0, LOTTO_NUMBERS_COUNT)
    }, null)
      .then(function (res) {
        if (res && res.ok && res.ticket) {
          highlightTicketId = res.ticket.id;
          setPurchaseStatus('Ticket purchased.');
          closeTicketPicker();
          return refreshState();
        }

        setTicketPickerStatus('Purchase failed.');
      })
      .catch(function (err) {
        setTicketPickerStatus(err.message);
      })
      .finally(function () {
        updatePickerUi();
        if (purchaseBtn) {
          purchaseBtn.disabled = !(latestState.currentDraw && latestState.currentDraw.state === 'active');
        }
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
      if (!r.ok) {
        return r.json().catch(function () { return null; }).then(function (body) {
          var msg = (body && body.error) ? body.error : ('HTTP ' + r.status);
          throw new Error(msg);
        });
      }

      return r.json();
    });
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
    if (!initData) return;

    setTimelineStatus('Loading timeline...');

    return postJson('/api/timeline', { initData: initData || '' }, null)
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
    if (!initData) return;
    if (!latestState.currentDraw || latestState.currentDraw.state !== 'active') {
      setPurchaseStatus('There is no active draw right now.');
      return;
    }

    setPurchaseStatus('');
    openTicketPicker();
  }

  if (purchaseBtn) purchaseBtn.addEventListener('click', purchaseTicket);
  if (lotteryTabBtn) lotteryTabBtn.addEventListener('click', function () { setActiveTab('lottery'); });
  if (accountTabBtn) accountTabBtn.addEventListener('click', function () { setActiveTab('account'); });
  if (openHistoryBtn) openHistoryBtn.addEventListener('click', openHistory);
  if (closeHistoryBtn) closeHistoryBtn.addEventListener('click', closeHistory);
  if (historyBackdropEl) historyBackdropEl.addEventListener('click', closeHistory);
  if (closeTicketPickerBtn) closeTicketPickerBtn.addEventListener('click', closeTicketPicker);
  if (ticketPickerBackdropEl) ticketPickerBackdropEl.addEventListener('click', closeTicketPicker);
  if (confirmTicketNumbersBtn) confirmTicketNumbersBtn.addEventListener('click', confirmSelectedTicketNumbers);

  setActiveTab('lottery');
  renderCurrentDraw(null);
  renderCurrentTickets([]);
  renderHistory([]);
  buildTicketPickerSlots();

  var hasTelegramInitData = false;
  try {
    hasTelegramInitData = !!(window.Telegram && Telegram.WebApp && Telegram.WebApp.initData && Telegram.WebApp.initData.length > 0);
  } catch (e) {
    hasTelegramInitData = false;
  }

  if (!hasTelegramInitData) {
    initData = 'local-debug';

    postJson('/api/auth/telegram', { initData: initData })
      .then(function () { return refreshState(); })
      .catch(function (err) {
        console.warn('Failed local debug auth', err);
        setPurchaseStatus(err.message);
      })
      .finally(function () {
        startPolling();
      });

    return;
  }

  startPolling();

  try {
    Telegram.WebApp.setHeaderColor('#0b1019');
    Telegram.WebApp.setBackgroundColor('#0b1019');
  } catch (e) {
  }

  Telegram.WebApp.ready();
  Telegram.WebApp.expand();

  try {
    initData = Telegram.WebApp.initData;
    postJson('/api/auth/telegram', { initData: initData })
      .then(function () { return refreshState(); })
      .catch(function (err) {
        console.warn('Failed to auth with Telegram initData', err);
        setPurchaseStatus(err.message);
      });
  } catch (e) {
    console.warn('Telegram auth initData error', e);
  }
})();
