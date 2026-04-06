(function () {
  var purchaseBtn = document.getElementById('purchaseTicketBtn');
  var purchaseStatusEl = document.getElementById('purchaseStatus');
  var timelineStatusEl = document.getElementById('timelineStatus');

  var lotteryTabBtn = document.getElementById('lotteryTabBtn');
  var ticketsTabBtn = document.getElementById('ticketsTabBtn');
  var profileTabBtn = document.getElementById('profileTabBtn');
  var lotteryTabPanel = document.getElementById('lotteryTabPanel');
  var ticketsTabPanel = document.getElementById('ticketsTabPanel');
  var profileTabPanel = document.getElementById('profileTabPanel');

  var currentDrawStateBadgeEl = document.getElementById('currentDrawStateBadge');
  var currentDrawEmptyEl = document.getElementById('currentDrawEmpty');
  var currentDrawContentEl = document.getElementById('currentDrawContent');
  var currentDrawIdEl = document.getElementById('currentDrawId');
  var currentDrawSubtitleEl = document.getElementById('currentDrawSubtitle');
  var currentDrawPrizePoolEl = document.getElementById('currentDrawPrizePool');
  var currentDrawCreatedAtEl = document.getElementById('currentDrawCreatedAt');
  var jackpotAmountEl = document.getElementById('jackpotAmount');
  var jackpotSubtitleEl = document.getElementById('jackpotSubtitle');
  var currentDrawPrizeTiersEl = document.getElementById('currentDrawPrizeTiers');
  var currentDrawPrizePool3El = document.getElementById('currentDrawPrizePool3');
  var currentDrawPrizePool4El = document.getElementById('currentDrawPrizePool4');
  var currentDrawPrizePool5El = document.getElementById('currentDrawPrizePool5');
  var currentDrawNumbersWrapEl = document.getElementById('currentDrawNumbersWrap');
  var currentDrawNumbersEl = document.getElementById('currentDrawNumbers');
  var currentDrawTicketPriceRowEl = document.getElementById('currentDrawTicketPriceRow');
  var currentDrawPurchaseBlockEl = document.getElementById('currentDrawPurchaseBlock');

  var myTicketsEmptyEl = document.getElementById('myTicketsEmpty');
  var myTicketsListEl = document.getElementById('myTicketsList');
  var debugModeBadgeEl = document.getElementById('debugModeBadge');

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
  var clientIsLocalDebug = false;
  var autoOpenedTicketsTab = false;
  var activeTabName = 'lottery';
  var pickerNumbers = [1, 2, 3, 4, 5];

  var LOTTO_NUMBERS_COUNT = 5;
  var LOTTO_MIN = 1;
  var LOTTO_MAX = 36;
  var PICKER_VALUE_RANGE = LOTTO_MAX - LOTTO_MIN + 1;
  var PICKER_SEED_OFFSET_MIN = 2;
  var PICKER_SEED_OFFSET_MAX = 5;
  var WHEEL_REPEAT_CYCLES = 7;

  var pickerWheels = [];
  var pickerWheelSnapTimers = [];
  var pickerWheelResizeBound = false;
  var pickerArrowHoldState = [];
  var wheelOrderDescending = true;
  var ticketPickerBuilt = false;
  var ticketPickerReady = false;
  var ticketPickerPendingOpen = false;
  var ticketPickerCloseTimer = null;
  var pickerApplyingSeed = false;
  var pickerOpenAnimationTimer = null;
  var pollingIntervalId = null;

  function setPurchaseStatus(text) {
    if (purchaseStatusEl) purchaseStatusEl.textContent = text || '';
  }

  function setTimelineStatus(text) {
    if (timelineStatusEl) timelineStatusEl.textContent = text || '';
  }

  function setDebugModeBadge(text) {
    if (!debugModeBadgeEl) return;

    var value = String(text || '').trim();
    debugModeBadgeEl.hidden = value.length === 0;
    debugModeBadgeEl.textContent = value;
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

  function randomizePickerNumbers() {
    var set = new Set();
    while (set.size < LOTTO_NUMBERS_COUNT) {
      set.add(LOTTO_MIN + Math.floor(Math.random() * PICKER_VALUE_RANGE));
    }

    pickerNumbers = Array.from(set).sort(function (a, b) { return a - b; });
  }

  function resetTicketPickerGrid() {
    if (ticketPickerGridEl) {
      ticketPickerGridEl.innerHTML = '';
    }

    pickerWheels = [];
    pickerWheelSnapTimers = [];
    ticketPickerBuilt = false;
    ticketPickerReady = false;
  }

  function preloadTicketPicker() {
    if (!ticketPickerBuilt) {
      buildTicketPickerSlots();
    }
  }

  function setWheelStartNearSeed(index) {
    var wheelState = pickerWheels[index];
    if (!wheelState || !wheelState.items || wheelState.items.length === 0) return;

    var seedValue = Math.min(LOTTO_MAX, Math.max(LOTTO_MIN, parseInt(pickerNumbers[index], 10) || LOTTO_MIN));
    var offset = PICKER_SEED_OFFSET_MIN + Math.floor(Math.random() * (PICKER_SEED_OFFSET_MAX - PICKER_SEED_OFFSET_MIN + 1));
    var direction = Math.random() < 0.5 ? -1 : 1;
    var startValue = seedValue + (offset * direction);

    if (startValue < LOTTO_MIN || startValue > LOTTO_MAX) {
      direction *= -1;
      startValue = seedValue + (offset * direction);
    }

    startValue = Math.min(LOTTO_MAX, Math.max(LOTTO_MIN, startValue));

    var itemIndex = wheelState.middleCycleStart + (wheelOrderDescending ? (LOTTO_MAX - startValue) : (startValue - LOTTO_MIN));
    itemIndex = Math.max(0, Math.min(wheelState.items.length - 1, itemIndex));

    centerWheelOnItem(wheelState, wheelState.items[itemIndex], 'auto');
  }

  function startPickerOpenAnimation() {
    if (!pickerApplyingSeed) return;

    if (pickerOpenAnimationTimer) {
      clearTimeout(pickerOpenAnimationTimer);
      pickerOpenAnimationTimer = null;
    }

    for (var i = 0; i < pickerWheels.length; i++) {
      var wheelState = pickerWheels[i];
      if (wheelState) wheelState.isOpening = true;
      normalizeWheelToMiddleCycle(i, 'smooth');
    }

    pickerOpenAnimationTimer = setTimeout(function () {
      pickerOpenAnimationTimer = null;
      for (var i = 0; i < pickerWheels.length; i++) {
        if (pickerWheels[i]) pickerWheels[i].isOpening = false;
      }
      pickerApplyingSeed = false;
      updatePickerUi();
    }, 403 + pickerWheels.length * 26);
  }

  function setSheetOpenClass() {
    var hasOpenSheet = !!(
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
    for (var i = 0; i < pickerWheels.length; i++) {
      renderWheelVisual(i);
    }

    var validation = validatePickerSelection();
    setTicketPickerStatus(validation.message);
    if (confirmTicketNumbersBtn) confirmTicketNumbersBtn.disabled = !validation.ok;
  }

  function getWheelNearestItemInfo(wheelState) {
    if (!wheelState || !wheelState.items || wheelState.items.length === 0) return null;

    var viewport = wheelState.viewport;
    var center = viewport.scrollTop + viewport.clientHeight / 2;
    var nearestIndex = 0;
    var nearestDistance = Number.POSITIVE_INFINITY;

    for (var i = 0; i < wheelState.items.length; i++) {
      var item = wheelState.items[i];
      var itemCenter = item.offsetTop + item.offsetHeight / 2;
      var distance = Math.abs(itemCenter - center);
      if (distance < nearestDistance) {
        nearestDistance = distance;
        nearestIndex = i;
      }
    }

    return {
      index: nearestIndex,
      item: wheelState.items[nearestIndex]
    };
  }

  function centerWheelOnItem(wheelState, item, behavior) {
    if (!wheelState || !item) return;
    var viewport = wheelState.viewport;
    var targetTop = item.offsetTop - (viewport.clientHeight - item.offsetHeight) / 2;
    viewport.scrollTo({ top: Math.max(0, targetTop), behavior: behavior || 'auto' });
  }

  function normalizeWheelToMiddleCycle(index, behavior) {
    var wheelState = pickerWheels[index];
    if (!wheelState || !wheelState.items || wheelState.items.length === 0) return;

    var selected = pickerNumbers[index];
    var inRange = Math.min(LOTTO_MAX, Math.max(LOTTO_MIN, parseInt(selected, 10) || LOTTO_MIN));
    var preferredItemIndex = wheelState.middleCycleStart + (wheelOrderDescending ? (LOTTO_MAX - inRange) : (inRange - LOTTO_MIN));
    preferredItemIndex = Math.max(0, Math.min(wheelState.items.length - 1, preferredItemIndex));
    centerWheelOnItem(wheelState, wheelState.items[preferredItemIndex], behavior || 'auto');
  }

  function computeWheelItemHeight(wheelState) {
    if (!wheelState || !wheelState.root) return 40;

    // Keep wheel inside the slot's middle row so status/confirm rows never get overlapped.
    var visibleHeight = wheelState.root.clientHeight || 0;
    var rowGap = wheelState.rowGap || 0;
    var target = visibleHeight > 0 ? Math.floor((visibleHeight - rowGap * 4) / 5) : 40;
    return Math.max(26, Math.min(56, target));
  }

  function applyWheelLayout(wheelState) {
    if (!wheelState || !wheelState.viewport || !wheelState.root) return;

    var visibleHeight = wheelState.root.clientHeight || 0;
    var rowGap = wheelState.rowGap || 0;
    var itemHeight = computeWheelItemHeight(wheelState);
    wheelState.itemHeight = itemHeight;
    wheelState.root.style.setProperty('--wheel-row-gap', rowGap.toFixed(2) + 'px');
    wheelState.root.style.setProperty('--wheel-item-height', itemHeight.toFixed(2) + 'px');
    wheelState.root.style.setProperty('--wheel-visible-height', (visibleHeight > 0 ? visibleHeight : itemHeight * 5).toFixed(2) + 'px');
  }

  function syncAllWheelLayouts() {
    for (var i = 0; i < pickerWheels.length; i++) {
      var wheelState = pickerWheels[i];
      if (!wheelState) continue;
      applyWheelLayout(wheelState);
      normalizeWheelToMiddleCycle(i, 'auto');
      renderWheelVisual(i);
    }
  }

  function renderWheelVisual(index) {
    var wheelState = pickerWheels[index];
    if (!wheelState || !wheelState.items || wheelState.items.length === 0) return;

    var viewport = wheelState.viewport;
    var center = viewport.scrollTop + viewport.clientHeight / 2;
    var nearestInfo = getWheelNearestItemInfo(wheelState);

    for (var i = 0; i < wheelState.items.length; i++) {
      var item = wheelState.items[i];
      var itemCenter = item.offsetTop + item.offsetHeight / 2;
      var distance = Math.abs(itemCenter - center);
      var normalized = Math.min(3.3, distance / wheelState.itemHeight);
      var direction = itemCenter < center ? -1 : 1;

      // Selected row is much larger/brighter than neighbors.
      var scale = Math.max(0.66, 1.62 - normalized * 0.28);
      var opacity = Math.max(0.08, 1 - normalized * 0.28);
      var blur = Math.max(0, (normalized - 0.55) * 0.9);
      var rotate = direction * Math.min(54, normalized * 20);

      item.style.transform = 'translateZ(0) rotateX(' + rotate.toFixed(2) + 'deg) scale(' + scale.toFixed(3) + ')';
      item.style.opacity = opacity.toFixed(3);
      item.style.filter = 'blur(' + blur.toFixed(2) + 'px)';
      item.classList.toggle('picker-wheel-selected', !!nearestInfo && i === nearestInfo.index);
    }

    if (!pickerApplyingSeed && nearestInfo && nearestInfo.item) {
      var nextValue = parseInt(nearestInfo.item.getAttribute('data-value'), 10);
      if (Number.isFinite(nextValue)) pickerNumbers[index] = nextValue;
    }
  }

  function scheduleWheelSnap(index) {
    if (pickerWheelSnapTimers[index]) {
      clearTimeout(pickerWheelSnapTimers[index]);
    }

    pickerWheelSnapTimers[index] = setTimeout(function () {
      var wheelState = pickerWheels[index];
      if (!wheelState) return;

      if (wheelState.isOpening) return;

      var nearestInfo = getWheelNearestItemInfo(wheelState);
      if (!nearestInfo || !nearestInfo.item) return;

      wheelState.isAutoScrolling = true;
      centerWheelOnItem(wheelState, nearestInfo.item, 'smooth');

      setTimeout(function () {
        wheelState.isAutoScrolling = false;

        var viewport = wheelState.viewport;
        var maxScrollTop = Math.max(0, (viewport.scrollHeight || 0) - (viewport.clientHeight || 0));
        var edgePadding = (wheelState.itemHeight || 40) * 4;
        if (viewport.scrollTop < edgePadding || viewport.scrollTop > maxScrollTop - edgePadding) {
          normalizeWheelToMiddleCycle(index, 'auto');
        }

        renderWheelVisual(index);
        updatePickerUi();
      }, 180);
    }, 90);
  }

  function stepPickerIndex(index, delta) {
    var wheelState = pickerWheels[index];
    if (!wheelState) {
      if (!Number.isFinite(index) || index < 0 || index >= pickerNumbers.length) return;
      pickerNumbers[index] = wrapPickerNumber(pickerNumbers[index] + delta);
      updatePickerUi();
      return;
    }

    var nearestInfo = getWheelNearestItemInfo(wheelState);
    if (!nearestInfo) return;

      var targetIndex = nearestInfo.index + (wheelOrderDescending ? -delta : delta);
    if (targetIndex < 0 || targetIndex >= wheelState.items.length) {
      normalizeWheelToMiddleCycle(index, 'auto');
      nearestInfo = getWheelNearestItemInfo(wheelState);
      if (!nearestInfo) return;
        targetIndex = nearestInfo.index + (wheelOrderDescending ? -delta : delta);
      targetIndex = Math.max(0, Math.min(wheelState.items.length - 1, targetIndex));
    }

    centerWheelOnItem(wheelState, wheelState.items[targetIndex], 'smooth');
    scheduleWheelSnap(index);
  }

  function stopArrowHold(button) {
    var state = button && button._pickerHoldState;
    if (!state) return;

    if (state.repeatTimer) clearInterval(state.repeatTimer);
    if (state.startTimer) clearTimeout(state.startTimer);
    state.repeatTimer = null;
    state.startTimer = null;
    state.active = false;
  }

  function bindArrowHold(button, stepFn) {
    if (!button) return;

    var state = { active: false, startTimer: null, repeatTimer: null };
    button._pickerHoldState = state;

    function beginHold(ev) {
      ev.preventDefault();
      if (state.active) return;

      state.active = true;
      stepFn();

      state.startTimer = setTimeout(function () {
        if (!state.active) return;
        state.repeatTimer = setInterval(function () {
          if (!state.active) return;
          stepFn();
        }, 100);
      }, 500);
    }

    function endHold() {
      stopArrowHold(button);
    }

    button.addEventListener('pointerdown', beginHold);
    button.addEventListener('pointerup', endHold);
    button.addEventListener('pointercancel', endHold);
    button.addEventListener('pointerleave', endHold);
    button.addEventListener('lostpointercapture', endHold);
    button.addEventListener('contextmenu', function (ev) { ev.preventDefault(); });
  }

  function buildTicketPickerSlots() {
    if (!ticketPickerGridEl) return;

    if (ticketPickerBuilt && ticketPickerGridEl.children.length > 0) {
      return;
    }

    ticketPickerReady = false;
    normalizePickerNumbers();
    ticketPickerGridEl.innerHTML = '';
    pickerWheels = [];
    pickerWheelSnapTimers = [];

    for (var i = 0; i < LOTTO_NUMBERS_COUNT; i++) {
      var slot = document.createElement('div');
      slot.className = 'picker-slot';

      var upBtn = document.createElement('button');
      upBtn.type = 'button';
      upBtn.className = 'picker-arrow';
      upBtn.textContent = '^';

      var value = document.createElement('div');
      value.className = 'picker-value picker-wheel';

      var focusBand = document.createElement('div');
      focusBand.className = 'picker-wheel-focus';

      var viewport = document.createElement('div');
      viewport.className = 'picker-wheel-viewport';
      viewport.setAttribute('data-picker-viewport', String(i));

      var track = document.createElement('div');
      track.className = 'picker-wheel-track';
      track.setAttribute('data-picker-track', String(i));

      for (var c = 0; c < WHEEL_REPEAT_CYCLES; c++) {
        if (wheelOrderDescending) {
          for (var vd = LOTTO_MAX; vd >= LOTTO_MIN; vd--) {
            var wheelItemD = document.createElement('div');
            wheelItemD.className = 'picker-wheel-item';
            wheelItemD.setAttribute('data-value', String(vd));
            wheelItemD.textContent = formatPickerNumber(vd);
            track.appendChild(wheelItemD);
          }
        } else {
          for (var va = LOTTO_MIN; va <= LOTTO_MAX; va++) {
            var wheelItemA = document.createElement('div');
            wheelItemA.className = 'picker-wheel-item';
            wheelItemA.setAttribute('data-value', String(va));
            wheelItemA.textContent = formatPickerNumber(va);
            track.appendChild(wheelItemA);
          }
        }
      }

      viewport.appendChild(track);
      value.appendChild(viewport);
      value.appendChild(focusBand);

      var downBtn = document.createElement('button');
      downBtn.type = 'button';
      downBtn.className = 'picker-arrow';
      downBtn.textContent = 'v';

      bindArrowHold(upBtn, (function (idx) {
        return function () { stepPickerIndex(idx, 1); };
      })(i));

      bindArrowHold(downBtn, (function (idx) {
        return function () { stepPickerIndex(idx, -1); };
      })(i));

      slot.appendChild(upBtn);
      slot.appendChild(value);
      slot.appendChild(downBtn);
      ticketPickerGridEl.appendChild(slot);

      // Delay wheel metrics until after layout is committed.
      (function (idx, rootEl, vp, tr) {
        requestAnimationFrame(function () {
          var items = tr.querySelectorAll('.picker-wheel-item');
          if (!items || items.length === 0) return;

          var wheelState = {
            root: rootEl,
            viewport: vp,
            track: tr,
            items: items,
            rowGap: 6,
            itemHeight: 40,
            middleCycleStart: Math.floor(WHEEL_REPEAT_CYCLES / 2) * PICKER_VALUE_RANGE
          };

          pickerWheels[idx] = wheelState;

          applyWheelLayout(wheelState);

          vp.addEventListener('scroll', function () {
            renderWheelVisual(idx);
            if (!wheelState.isAutoScrolling) {
              scheduleWheelSnap(idx);
            }
          }, { passive: true });

          // Ensure desktop mouse wheel changes the picker even when nested in sheets.
          rootEl.addEventListener('wheel', function (e) {
            e.preventDefault();
            vp.scrollTop += e.deltaY;
            if (wheelState.isOpening) return;
            renderWheelVisual(idx);
            scheduleWheelSnap(idx);
          }, { passive: false });

          if (pickerApplyingSeed) {
            setWheelStartNearSeed(idx);
          } else {
            normalizeWheelToMiddleCycle(idx, 'auto');
            renderWheelVisual(idx);
          }

          if (!ticketPickerReady && pickerWheels.filter(Boolean).length === LOTTO_NUMBERS_COUNT) {
            ticketPickerReady = true;
            if (ticketPickerPendingOpen) {
              ticketPickerPendingOpen = false;
              requestAnimationFrame(function () {
                beginPickerOpenSequence();
              });
            }
          }
        });
      })(i, value, viewport, track);
    }

    if (!pickerWheelResizeBound) {
      pickerWheelResizeBound = true;
      window.addEventListener('resize', function () {
        syncAllWheelLayouts();
      });
    }

    ticketPickerBuilt = true;

    if (!pickerApplyingSeed) {
      updatePickerUi();
    }
  }

  function createNumbersRow(numbersStr, drawNumbersStr) {
    var numbersWrap = document.createElement('div');
    numbersWrap.className = 'ticket-numbers';

    var resultLookup = {};
    parseNumbers(drawNumbersStr).forEach(function (n) {
      resultLookup[n] = true;
    });
    var hasResult = Object.keys(resultLookup).length > 0;

    parseNumbers(numbersStr).forEach(function (n) {
      var ball = document.createElement('div');
      ball.className = 'ball';
      if (hasResult) {
        ball.classList.add('ball-result-ready');
        if (resultLookup[n]) ball.classList.add('ball-match');
      }
      ball.textContent = String(n);
      numbersWrap.appendChild(ball);
    });

    return numbersWrap;
  }

  function getTicketStatusMeta(status) {
    var value = String(status || '').toLowerCase();
    if (value === 'winnings_available') return { label: 'Winnings available', className: 'ticket-status-win' };
    if (value === 'winnings_claimed') return { label: 'Winnings claimed', className: 'ticket-status-claimed' };
    if (value === 'expired_no_win') return { label: 'Expired', className: 'ticket-status-expired' };
    return { label: 'Waiting for draw', className: 'ticket-status-awaiting' };
  }

  function createTicketEl(ticket, draw) {
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

    var statusMeta = getTicketStatusMeta(ticket && ticket.status);
    var statusBadge = document.createElement('div');
    statusBadge.className = 'ticket-status ' + statusMeta.className;
    statusBadge.textContent = statusMeta.label;

    header.appendChild(title);
    header.appendChild(statusBadge);

    el.appendChild(header);
    el.appendChild(createNumbersRow(ticket.numbers, draw && draw.numbers ? draw.numbers : null));
    el.appendChild(meta);

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
        ticketsWrap.appendChild(createTicketEl(ticket, draw));
      });
      container.appendChild(ticketsWrap);
    }

    return container;
  }

  function renderCurrentDraw(draw, currentTickets) {
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
      if (currentDrawPrizeTiersEl) currentDrawPrizeTiersEl.hidden = true;
      if (currentDrawPrizePool3El) currentDrawPrizePool3El.textContent = '$0.00';
      if (currentDrawPrizePool4El) currentDrawPrizePool4El.textContent = '$0.00';
      if (currentDrawPrizePool5El) currentDrawPrizePool5El.textContent = '$0.00';
      if (purchaseBtn) {
        purchaseBtn.disabled = true;
        purchaseBtn.hidden = false;
        purchaseBtn.title = 'No active draw available.';
      }
      if (currentDrawSubtitleEl) currentDrawSubtitleEl.hidden = false;
      if (currentDrawTicketPriceRowEl) currentDrawTicketPriceRowEl.hidden = false;
      if (currentDrawPurchaseBlockEl) currentDrawPurchaseBlockEl.hidden = false;
      return;
    }

    var isFinishedDraw = draw.state === 'finished';

    currentDrawStateBadgeEl.textContent = draw.state;
    currentDrawStateBadgeEl.className = 'state-badge ' + (draw.state === 'active' ? 'state-badge-active' : draw.state === 'finished' ? 'state-badge-finished' : 'state-badge-upcoming');
    currentDrawEmptyEl.hidden = true;
    currentDrawContentEl.hidden = false;

    if (currentDrawIdEl) currentDrawIdEl.textContent = 'PowerBall Global • #' + draw.id;
    if (currentDrawPrizePoolEl) currentDrawPrizePoolEl.textContent = '$' + formatPrizePool(draw.prizePool);
    if (currentDrawCreatedAtEl) currentDrawCreatedAtEl.textContent = (draw.state === 'finished' ? 'Concluded ' : 'Opened ') + formatUtc(draw.createdAtUtc);
    if (jackpotAmountEl) jackpotAmountEl.textContent = formatJackpot(draw.prizePool);
    if (currentDrawPrizeTiersEl) currentDrawPrizeTiersEl.hidden = false;
    if (currentDrawPrizePool3El) currentDrawPrizePool3El.textContent = '$' + formatPrizePool(draw.prizePoolMatch3);
    if (currentDrawPrizePool4El) currentDrawPrizePool4El.textContent = '$' + formatPrizePool(draw.prizePoolMatch4);
    if (currentDrawPrizePool5El) currentDrawPrizePool5El.textContent = '$' + formatPrizePool(draw.prizePoolMatch5);

    var hasWinningsAvailable = false;
    (currentTickets || []).forEach(function (ticket) {
      if (ticket && ticket.status === 'winnings_available') hasWinningsAvailable = true;
    });

    if (jackpotSubtitleEl) {
      if (draw.state === 'active') {
        jackpotSubtitleEl.textContent = 'The draw is live. Don\'t miss your chance to become a multi-millionaire!';
      } else if (draw.state === 'finished' && hasWinningsAvailable) {
        jackpotSubtitleEl.textContent = 'This draw is finished and your winnings are ready to claim.';
      } else if (draw.state === 'finished') {
        jackpotSubtitleEl.textContent = 'This draw is finished. Check your tickets against the result numbers.';
      } else {
        jackpotSubtitleEl.textContent = 'The next draw is coming soon. Get your tickets now.';
      }
    }

    var hasNumbers = !!(draw.numbers && String(draw.numbers).length > 0);
    if (currentDrawNumbersWrapEl) currentDrawNumbersWrapEl.hidden = !hasNumbers;
    if (currentDrawNumbersEl) {
      currentDrawNumbersEl.innerHTML = '';
      if (hasNumbers) currentDrawNumbersEl.appendChild(createNumbersRow(draw.numbers, draw.numbers));
    }

    if (purchaseBtn) {
      purchaseBtn.disabled = draw.state !== 'active';
      purchaseBtn.hidden = isFinishedDraw;
      purchaseBtn.title = draw.state === 'active' ? '' : 'Only the active draw accepts purchases.';
    }

    if (currentDrawSubtitleEl) currentDrawSubtitleEl.hidden = isFinishedDraw;
    if (currentDrawTicketPriceRowEl) currentDrawTicketPriceRowEl.hidden = isFinishedDraw;
    if (currentDrawPurchaseBlockEl) currentDrawPurchaseBlockEl.hidden = isFinishedDraw;
  }

  function buildMyTicketGroups(state) {
    var groups = [];
    var seenDraw = {};

    var currentDraw = state && state.currentDraw ? state.currentDraw : null;
    var currentTickets = state && state.currentTickets ? state.currentTickets : [];
    if (currentDraw && currentTickets.length > 0) {
      groups.push({ drawId: currentDraw.id, draw: currentDraw, tickets: currentTickets.slice() });
      seenDraw[currentDraw.id] = true;
    }

    var history = state && state.history ? state.history : [];
    history.forEach(function (group) {
      if (!group) return;
      if (seenDraw[group.drawId]) return;
      if (!group.tickets || group.tickets.length === 0) return;
      groups.push(group);
      seenDraw[group.drawId] = true;
    });

    groups.sort(function (a, b) {
      return (Number(b.drawId) || 0) - (Number(a.drawId) || 0);
    });

    return groups;
  }

  function renderMyTickets(state) {
    if (!myTicketsListEl || !myTicketsEmptyEl) return;

    myTicketsListEl.innerHTML = '';

    var groups = buildMyTicketGroups(state);
    myTicketsEmptyEl.hidden = groups.length > 0;

    groups.forEach(function (group) {
      myTicketsListEl.appendChild(createHistoryGroupEl(group));
    });
  }

  function setActiveTab(name) {
    var tab = name || 'lottery';
    activeTabName = tab;
    var isLottery = tab === 'lottery';
    var isTickets = tab === 'tickets';
    var isProfile = tab === 'profile';

    if (lotteryTabPanel) lotteryTabPanel.hidden = !isLottery;
    if (ticketsTabPanel) ticketsTabPanel.hidden = !isTickets;
    if (profileTabPanel) profileTabPanel.hidden = !isProfile;

    if (lotteryTabBtn) lotteryTabBtn.classList.toggle('tabbar-btn-active', isLottery);
    if (ticketsTabBtn) ticketsTabBtn.classList.toggle('tabbar-btn-active', isTickets);
    if (profileTabBtn) profileTabBtn.classList.toggle('tabbar-btn-active', isProfile);

    if (isTickets) {
      renderMyTickets(latestState);
    }
  }

  function openTicketPicker() {
    if (!ticketPickerSheetEl) return;

    if (!ticketPickerReady) {
      ticketPickerPendingOpen = true;
      setTicketPickerStatus('Preparing numbers...');
      preloadTicketPicker();
      return;
    }

    beginPickerOpenSequence();
  }

  function beginPickerOpenSequence() {
    if (!ticketPickerSheetEl || !ticketPickerReady) return;

    if (ticketPickerCloseTimer) {
      clearTimeout(ticketPickerCloseTimer);
      ticketPickerCloseTimer = null;
    }

    ticketPickerSheetEl.hidden = false;
    ticketPickerSheetEl.classList.remove('ticket-picker-closing');
    ticketPickerSheetEl.offsetHeight;
    ticketPickerSheetEl.classList.add('ticket-picker-open');
    setSheetOpenClass();

    requestAnimationFrame(function () {
      if (pickerOpenAnimationTimer) {
        clearTimeout(pickerOpenAnimationTimer);
        pickerOpenAnimationTimer = null;
      }

      randomizePickerNumbers();
      pickerApplyingSeed = true;

      for (var i = 0; i < pickerWheels.length; i++) {
        setWheelStartNearSeed(i);
      }

      pickerOpenAnimationTimer = setTimeout(function () {
        pickerOpenAnimationTimer = null;
        startPickerOpenAnimation();
      }, 72);
    });
  }

  function closeTicketPicker() {
    if (!ticketPickerSheetEl) return;

    if (pickerOpenAnimationTimer) {
      clearTimeout(pickerOpenAnimationTimer);
      pickerOpenAnimationTimer = null;
    }

    pickerApplyingSeed = false;

    ticketPickerSheetEl.classList.remove('ticket-picker-open');
    ticketPickerSheetEl.classList.add('ticket-picker-closing');
    setSheetOpenClass();

    if (ticketPickerCloseTimer) clearTimeout(ticketPickerCloseTimer);
    ticketPickerCloseTimer = setTimeout(function () {
      ticketPickerSheetEl.hidden = true;
      ticketPickerSheetEl.classList.remove('ticket-picker-closing');
      ticketPickerCloseTimer = null;
      setSheetOpenClass();
    }, 260);
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
          var err = new Error(msg);
          err.status = r.status;
          throw err;
        });
      }

      return r.json();
    });
  }

  function handleAuthFailure(err) {
    var status = err && err.status;
    if (status === 401) {
      setTimelineStatus('Authentication failed. Open this app from Telegram, or use the local debug profile in Development.');
      setPurchaseStatus('Authentication failed.');
      return;
    }

    setPurchaseStatus(err && err.message ? err.message : 'Authentication failed.');
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
          return {
            id: ticket.id,
            drawId: ticket.drawId,
            numbers: ticket.numbers || null,
            status: ticket.status || null,
            purchasedAtUtc: ticket.purchasedAtUtc || null
          };
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
              return {
                id: ticket.id,
                numbers: ticket.numbers || null,
                status: ticket.status || null,
                purchasedAtUtc: ticket.purchasedAtUtc || null
              };
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

    renderCurrentDraw(latestState.currentDraw || null, latestState.currentTickets || []);
    renderMyTickets(latestState);

    if (clientIsLocalDebug && !autoOpenedTicketsTab) {
      var ticketGroups = buildMyTicketGroups(latestState);
      if (ticketGroups.length > 0 && activeTabName !== 'tickets') {
        autoOpenedTicketsTab = true;
        setActiveTab('tickets');
      }
    }
  }

  function refreshState() {
    if (!initData) return;


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
    if (pollingIntervalId) return;

    try {
      pollingIntervalId = setInterval(function () {
        refreshState();
      }, 4000);
    } catch (e) {
      pollingIntervalId = null;
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
  if (ticketsTabBtn) ticketsTabBtn.addEventListener('click', function () { setActiveTab('tickets'); });
  if (profileTabBtn) profileTabBtn.addEventListener('click', function () { setActiveTab('profile'); });
  if (closeTicketPickerBtn) closeTicketPickerBtn.addEventListener('click', closeTicketPicker);
  if (ticketPickerBackdropEl) ticketPickerBackdropEl.addEventListener('click', closeTicketPicker);
  if (confirmTicketNumbersBtn) confirmTicketNumbersBtn.addEventListener('click', confirmSelectedTicketNumbers);

  preloadTicketPicker();
  setActiveTab('lottery');
  renderCurrentDraw(null, []);
  renderMyTickets({ currentDraw: null, currentTickets: [], history: [] });

  var search = '';
  try {
    search = window.location && window.location.search ? window.location.search : '';
  } catch (e) {
    search = '';
  }

  var query = new URLSearchParams(search || '');
  var forceLocalDebug = query.get('debug') === '1' || query.get('mode') === 'local-debug';

  var hasTelegramInitData = false;
  try {
    hasTelegramInitData = !!(window.Telegram && Telegram.WebApp && Telegram.WebApp.initData && Telegram.WebApp.initData.length > 0);
  } catch (e) {
    hasTelegramInitData = false;
  }

  if (forceLocalDebug || !hasTelegramInitData) {
    clientIsLocalDebug = true;
    setDebugModeBadge(forceLocalDebug
      ? 'Client debug mode: forced local debug via query parameter.'
      : 'Client debug mode: Telegram initData is missing, using local debug.');

    initData = 'local-debug';

    postJson('/api/auth/telegram', { initData: initData })
      .then(function () { return refreshState(); })
      .then(function () {
        startPolling();
      })
      .catch(function (err) {
        console.warn('Failed local debug auth', err);
        handleAuthFailure(err);
      });

    return;
  }

  setDebugModeBadge('');
  clientIsLocalDebug = false;


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
      .then(function () {
        startPolling();
      })
      .catch(function (err) {
        console.warn('Failed to auth with Telegram initData', err);
        handleAuthFailure(err);
      });
  } catch (e) {
    console.warn('Telegram auth initData error', e);
  }
})();
