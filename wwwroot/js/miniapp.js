(function () {
  var purchaseBtn = document.getElementById('purchaseTicketBtn');
  var purchaseStatusEl = document.getElementById('purchaseStatus');
  var timelineStatusEl = document.getElementById('timelineStatus');
  var topUpBtn = document.getElementById('topUpBtn');
  var topUpStatusEl = document.getElementById('topUpStatus');
  var topUpAmountInputEl = document.getElementById('topUpAmountInput');
  var promoCodeInputEl = document.getElementById('promoCodeInput');
  var applyPromoBtn = document.getElementById('applyPromoBtn');
  var promoStatusEl = document.getElementById('promoStatus');
  var copyReferralLinkBtn = document.getElementById('copyReferralLinkBtn');
  var referralCodeTextEl = document.getElementById('referralCodeText');
  var referralStatusEl = document.getElementById('referralStatus');
  var withdrawBtn = document.getElementById('withdrawBtn');
  var withdrawStatusEl = document.getElementById('withdrawStatus');
  var withdrawAmountInputEl = document.getElementById('withdrawAmountInput');
  var withdrawNumberInputEl = document.getElementById('withdrawNumberInput');
  var walletAddressInputEl = document.getElementById('walletAddressInput');
  var saveWalletAddressBtn = document.getElementById('saveWalletAddressBtn');
  var walletAddressStatusEl = document.getElementById('walletAddressStatus');
  var openDepositScreenBtn = document.getElementById('openDepositScreenBtn');
  var openInviteScreenBtn = document.getElementById('openInviteScreenBtn');
  var openWithdrawScreenBtn = document.getElementById('openWithdrawScreenBtn');
  var openHistoryScreenBtn = document.getElementById('openHistoryScreenBtn');
  var backFromDepositBtn = document.getElementById('backFromDepositBtn');
  var backFromInviteBtn = document.getElementById('backFromInviteBtn');
  var backFromWithdrawBtn = document.getElementById('backFromWithdrawBtn');
  var closeHistoryBtn = document.getElementById('closeHistoryBtn');
  var profileHomeScreenEl = document.getElementById('profileHomeScreen');
  var profileDepositScreenEl = document.getElementById('profileDepositScreen');
  var profileInviteScreenEl = document.getElementById('profileInviteScreen');
  var profileWithdrawScreenEl = document.getElementById('profileWithdrawScreen');
  var historyScreenEl = document.getElementById('profileHistoryScreen');
  var historyStatusEl = document.getElementById('historyStatus');
  var historyEmptyEl = document.getElementById('historyEmpty');
  var historyListEl = document.getElementById('historyList');
  var userBalanceTextEl = document.getElementById('userBalanceText');
  var profileBalanceTextEl = document.getElementById('profileBalanceText');

  var lotteryTabBtn = document.getElementById('lotteryTabBtn');
  var ticketsTabBtn = document.getElementById('ticketsTabBtn');
  var ticketsWinningPinEl = document.getElementById('ticketsWinningPin');
  var profileTabBtn = document.getElementById('profileTabBtn');
  var lotteryTabPanel = document.getElementById('lotteryTabPanel');
  var ticketsTabPanel = document.getElementById('ticketsTabPanel');
  var profileTabPanel = document.getElementById('profileTabPanel');

  var currentDrawStateBadgeEl = document.getElementById('currentDrawStateBadge') || document.getElementById('jackpotGameStateBadge');
  var currentDrawEmptyEl = document.getElementById('currentDrawEmpty');
  var currentDrawContentEl = document.getElementById('currentDrawContent');
  // previous currentDrawId/currentDrawSubtitle were removed; use jackpotGameTitle/subtitle instead
  var currentDrawIdEl = document.getElementById('jackpotGameTitle') || document.getElementById('currentDrawId');
  var currentDrawSubtitleEl = document.getElementById('jackpotGameSubtitle') || document.getElementById('currentDrawSubtitle');
  var currentDrawPrizePoolEl = document.getElementById('currentDrawPrizePool');
  var currentDrawCreatedAtEl = document.getElementById('currentDrawCreatedAt');
  var jackpotChipEl = document.querySelector('.jackpot-chip');
  var jackpotAmountEl = document.getElementById('jackpotAmount');
  var jackpotSubtitleEl = document.getElementById('jackpotSubtitle');
  var currentDrawPrizeTiersEl = document.getElementById('currentDrawPrizeTiers');
  var currentDrawPrizePool3El = document.getElementById('currentDrawPrizePool3');
  var currentDrawPrizePool4El = document.getElementById('currentDrawPrizePool4');
  var currentDrawPrizePool5El = document.getElementById('currentDrawPrizePool5');
  var currentDrawNumbersWrapEl = document.getElementById('currentDrawNumbersWrap');
  var currentDrawNumbersEl = document.getElementById('currentDrawNumbers');
  var currentDrawTicketPriceRowEl = document.getElementById('currentDrawTicketPriceRow');
  var currentDrawTicketCostEl = document.getElementById('currentDrawTicketCost');
  var currentDrawPurchaseBlockEl = document.getElementById('currentDrawPurchaseBlock');
  var featuredJackpotCardEl = document.getElementById('featuredJackpotCard');
  var jackpotCardsContainerEl = document.getElementById('jackpotCardsContainer');
  var currentDisplayedDrawId = null;
  var appShellEl = document.getElementById('appShell');
  var appLoadingShellEl = document.getElementById('appLoadingShell');

  var myTicketsEmptyEl = document.getElementById('myTicketsEmpty');
  var myTicketsListEl = document.getElementById('myTicketsList');
  var debugModeBadgeEl = document.getElementById('debugModeBadge');

  var ticketPickerSheetEl = document.getElementById('ticketPickerSheet');
  var ticketPickerBackdropEl = document.getElementById('ticketPickerBackdrop');
  var closeTicketPickerBtn = document.getElementById('closeTicketPickerBtn');
  var ticketPickerGridEl = document.getElementById('ticketPickerGrid');
  var ticketPickerStatusEl = document.getElementById('ticketPickerStatus');
  var confirmTicketNumbersBtn = document.getElementById('confirmTicketNumbersBtn');
  var centerPopupEl = document.getElementById('centerPopup');
  var centerPopupBackdropEl = document.getElementById('centerPopupBackdrop');
  var centerPopupMessageEl = document.getElementById('centerPopupMessage');
  var centerPopupConfirmBtn = document.getElementById('centerPopupConfirmBtn');

  var highlightTicketId = null;
  var lastStateSig = null;
  var latestState = { balance: 0, currentDraw: null, activeDraws: [], activeTicketGroups: [], currentTickets: [], history: [] };
  var selectedActiveDrawId = null;
  var appHasLoadedState = false;
  var initData = null;
  var clientIsLocalDebug = false;
  var localeCode = 'en';
  var localeVersion = '0';
  var localeStrings = {};
  var autoOpenedTicketsTab = false;
  var activeTabName = 'lottery';
  var activeProfileScreen = 'home';
  var historyEntries = [];
  var referralInviteCode = '';
  var referralInviteLink = '';
  var referralCodeFromQuery = '';
  var referralIsBound = false;
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
  var pickerHasAnimatedOpen = false;
  var pickerWheelSyncLockedUntil = 0;
  var pickerOpenAnimationTimer = null;
  var pollingIntervalId = null;
  var debugModeBadgeReason = '';
  var localDebugWatchdog = null;

  function getDebugModeBadgeText(reason) {
    if (reason === 'forced-local') {
      return t('client.debug.forcedLocal', 'Client debug mode: forced local debug via query parameter.');
    }

    if (reason === 'missing-init-data') {
      return t('client.debug.missingInitData', 'Client debug mode: Telegram initData is missing, using local debug.');
    }

    return '';
  }

  function reapplyLocalizedRuntimeTexts() {
    var selected = resolveSelectedDrawSnapshot(latestState);
    renderCurrentDraw(selected.draw, selected.tickets, selected.hasMultipleActiveDraws);
    renderActiveDrawBanners(getActiveDraws(latestState), getActiveTicketGroups(latestState));
    renderMyTickets(latestState);
    renderHistory();

    if (clientIsLocalDebug && debugModeBadgeReason) {
      setDebugModeBadge(getDebugModeBadgeText(debugModeBadgeReason));
    }
  }

  function getActiveDraws(state) {
    return (state && Array.isArray(state.activeDraws)) ? state.activeDraws : [];
  }

  function getActiveTicketGroups(state) {
    return (state && Array.isArray(state.activeTicketGroups)) ? state.activeTicketGroups : [];
  }

  function resolveSelectedDrawSnapshot(state) {
    var activeDraws = getActiveDraws(state);
    var hasMultipleActiveDraws = activeDraws.length > 1;

    var selectedDraw = null;
    if (selectedActiveDrawId != null) {
      selectedDraw = activeDraws.find(function (x) { return x && x.id === selectedActiveDrawId; }) || null;
    }

    if (!selectedDraw && activeDraws.length > 0) {
      selectedDraw = activeDraws[0];
    }

    if (!selectedDraw) {
      selectedDraw = state && state.currentDraw ? state.currentDraw : null;
    }

    var tickets = [];
    if (selectedDraw && activeDraws.some(function (x) { return x && x.id === selectedDraw.id; })) {
      var activeGroup = getActiveTicketGroups(state).find(function (group) {
        return group && group.drawId === selectedDraw.id;
      });
      tickets = activeGroup && Array.isArray(activeGroup.tickets) ? activeGroup.tickets : [];
    } else {
      tickets = (state && Array.isArray(state.currentTickets)) ? state.currentTickets : [];
    }

    return { draw: selectedDraw, tickets: tickets, hasMultipleActiveDraws: hasMultipleActiveDraws };
  }

  // Per-draw banner UI removed — jackpot card will show featured/current draw and contain the purchase controls.

  function setPurchaseStatus(text) {
    if (purchaseStatusEl) purchaseStatusEl.textContent = text || '';
  }

  function setTimelineStatus(text) {
    if (timelineStatusEl) timelineStatusEl.textContent = text || '';
  }

  function setTopUpStatus(text) {
    if (topUpStatusEl) topUpStatusEl.textContent = text || '';
  }

  function setPromoStatus(text) {
    if (promoStatusEl) promoStatusEl.textContent = text || '';
  }

  function setReferralStatus(text) {
    if (referralStatusEl) referralStatusEl.textContent = text || '';
  }

  function setWithdrawStatus(text) {
    if (withdrawStatusEl) withdrawStatusEl.textContent = text || '';
  }

  function setWalletAddressStatus(text) {
    if (walletAddressStatusEl) walletAddressStatusEl.textContent = text || '';
  }

  function setHistoryStatus(text) {
    if (historyStatusEl) historyStatusEl.textContent = text || '';
  }

  function setDebugModeBadge(text) {
    if (!debugModeBadgeEl) return;

    var value = String(text || '').trim();
    debugModeBadgeEl.hidden = value.length === 0;
    debugModeBadgeEl.textContent = value;
  }

  function t(key, fallback) {
    var value = localeStrings && Object.prototype.hasOwnProperty.call(localeStrings, key)
      ? localeStrings[key]
      : null;

    if (value == null || String(value).trim().length === 0) return String(fallback || '');
    return String(value);
  }

  function applyLocalizedDomTexts() {
    var nodes = document.querySelectorAll('[data-loc]');
    for (var i = 0; i < nodes.length; i++) {
      var node = nodes[i];
      var key = node.getAttribute('data-loc');
      if (!key) continue;
      var fallback = node.textContent || '';
      node.textContent = t(key, fallback);
    }

    var placeholderNodes = document.querySelectorAll('[data-loc-placeholder]');
    for (var j = 0; j < placeholderNodes.length; j++) {
      var placeholderNode = placeholderNodes[j];
      var placeholderKey = placeholderNode.getAttribute('data-loc-placeholder');
      if (!placeholderKey) continue;
      var fallbackPlaceholder = placeholderNode.getAttribute('placeholder') || '';
      placeholderNode.setAttribute('placeholder', t(placeholderKey, fallbackPlaceholder));
    }

    var ariaNodes = document.querySelectorAll('[data-loc-aria-label]');
    for (var k = 0; k < ariaNodes.length; k++) {
      var ariaNode = ariaNodes[k];
      var ariaKey = ariaNode.getAttribute('data-loc-aria-label');
      if (!ariaKey) continue;
      var fallbackAria = ariaNode.getAttribute('aria-label') || '';
      ariaNode.setAttribute('aria-label', t(ariaKey, fallbackAria));
    }

    try {
      document.documentElement.lang = localeCode || 'en';
    } catch (e) {
    }

    reapplyLocalizedRuntimeTexts();
  }

  function loadLocaleFromCache() {
    try {
      var raw = localStorage.getItem('miniapp.locale.bootstrap.v1');
      if (!raw) return;

      var parsed = JSON.parse(raw);
      if (!parsed || !parsed.locale || !parsed.strings) return;

      localeCode = String(parsed.locale || 'en');
      localeVersion = String(parsed.version || '0');
      localeStrings = parsed.strings || {};
      applyLocalizedDomTexts();
    } catch (e) {
    }
  }

  function saveLocaleToCache() {
    try {
      localStorage.setItem('miniapp.locale.bootstrap.v1', JSON.stringify({
        locale: localeCode,
        version: localeVersion,
        strings: localeStrings
      }));
    } catch (e) {
    }
  }

  function loadRemoteLocale(initDataValue) {
    return postJson('/api/localization/bootstrap', {
      initData: initDataValue || '',
      locale: localeCode
    }, null)
      .then(function (res) {
        if (!(res && res.ok && res.locale && res.strings)) return;

        localeCode = String(res.locale || 'en');
        localeVersion = String(res.version || '0');
        localeStrings = res.strings || {};
        saveLocaleToCache();
        applyLocalizedDomTexts();
      })
      .catch(function () {
        // Keep cached/fallback locale silently.
      });
  }

  function markAppReady() {
    if (appHasLoadedState) return;
    appHasLoadedState = true;

    if (document.body) {
      document.body.classList.remove('app-loading');
      document.body.classList.add('app-ready');
    }

    if (appShellEl) {
      appShellEl.setAttribute('aria-busy', 'false');
    }

    if (appLoadingShellEl) {
      appLoadingShellEl.setAttribute('aria-hidden', 'true');
    }
  }

  function getIntlLocale() {
    if (localeCode === 'ru') return 'ru-RU';
    if (localeCode === 'uz') return 'uz-UZ';
    return 'en-US';
  }

  function formatUtc(iso) {
    try {
      return new Date(iso).toLocaleString(getIntlLocale());
    } catch (e) {
      return String(iso || '');
    }
  }

  function formatPrizePool(value) {
    var amount = Number(value || 0);
    if (!Number.isFinite(amount)) amount = 0;
    return amount.toLocaleString(getIntlLocale(), { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  function formatPrizeTierPool(value) {
    var amount = Number(value || 0);
    if (!Number.isFinite(amount)) amount = 0;
    return Math.round(amount).toLocaleString(getIntlLocale(), { maximumFractionDigits: 0 });
  }

  function formatJackpot(value) {
    var amount = Number(value || 0);
    if (!Number.isFinite(amount)) amount = 0;
    return '$' + Math.round(amount).toLocaleString(getIntlLocale());
  }

  function formatCurrency(value) {
    var amount = Number(value || 0);
    if (!Number.isFinite(amount)) amount = 0;
    return '$' + amount.toLocaleString(getIntlLocale(), { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  function formatWinningsCurrency(value) {
    var amount = Number(value || 0);
    if (!Number.isFinite(amount)) amount = 0;
    return '$' + Math.round(amount).toLocaleString(getIntlLocale(), { maximumFractionDigits: 0 });
  }

  function renderBalance(balanceValue) {
    var formatted = formatCurrency(balanceValue);
    if (userBalanceTextEl) userBalanceTextEl.textContent = formatted;
    if (profileBalanceTextEl) profileBalanceTextEl.textContent = formatted;
  }

  function setTicketsWinningPin(count) {
    if (!ticketsWinningPinEl) return;

    var amount = Math.max(0, parseInt(count, 10) || 0);
    ticketsWinningPinEl.hidden = amount <= 0;
    ticketsWinningPinEl.textContent = amount > 99 ? '99+' : String(amount);
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

  function showCenterPopup(message) {
    if (!centerPopupEl || !centerPopupMessageEl) return;

    centerPopupMessageEl.textContent = String(message || '').trim() || t('client.popup.defaultError', 'Action cannot be completed.');
    centerPopupEl.hidden = false;
  }

  function hideCenterPopup() {
    if (!centerPopupEl) return;
    centerPopupEl.hidden = true;
  }

  function shouldShowInvalidTicketPopup(message) {
    var value = String(message || '').toLowerCase();
    return value.indexOf('already purchased this ticket') >= 0
      || value.indexOf('numbers must be unique') >= 0
      || value.indexOf('choose 5 unique numbers') >= 0;
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

    lockPickerWheelSync(650);

    for (var i = 0; i < pickerWheels.length; i++) {
      var wheelState = pickerWheels[i];
      if (wheelState) wheelState.isOpening = true;
      normalizeWheelToMiddleCycle(i, 'smooth');
    }

    pickerOpenAnimationTimer = setTimeout(function () {
      pickerOpenAnimationTimer = null;
      lockPickerWheelSync(180);
      for (var i = 0; i < pickerWheels.length; i++) {
        if (pickerWheels[i]) pickerWheels[i].isOpening = false;
      }
      pickerApplyingSeed = false;
      updatePickerUi();
    }, 403 + pickerWheels.length * 26);
  }

  function restorePickerWheelPositions() {
    for (var i = 0; i < pickerWheels.length; i++) {
      normalizeWheelToMiddleCycle(i, 'auto');
    }
  }

  function lockPickerWheelSync(ms) {
    pickerWheelSyncLockedUntil = Math.max(pickerWheelSyncLockedUntil, Date.now() + Math.max(0, ms || 0));
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

    if (duplicates) return { ok: false, message: t('client.picker.chooseUnique', 'Please choose 5 unique numbers.') };
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

    if ((behavior || 'auto') === 'smooth') {
      viewport.scrollTo({ top: Math.max(0, targetTop), behavior: 'smooth' });
      return;
    }

    viewport.scrollTop = Math.max(0, targetTop);
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

  function commitPickerNumberFromWheel(index) {
    var wheelState = pickerWheels[index];
    if (!wheelState || !wheelState.items || wheelState.items.length === 0) return;

    var nearestInfo = getWheelNearestItemInfo(wheelState);
    if (!nearestInfo || !nearestInfo.item) return;

    var nextValue = parseInt(nearestInfo.item.getAttribute('data-value'), 10);
    if (Number.isFinite(nextValue)) pickerNumbers[index] = nextValue;
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

  }

  function scheduleWheelSnap(index) {
    if (pickerWheelSnapTimers[index]) {
      clearTimeout(pickerWheelSnapTimers[index]);
    }

    pickerWheelSnapTimers[index] = setTimeout(function () {
      var wheelState = pickerWheels[index];
      if (!wheelState) return;

      if (wheelState.isOpening || Date.now() < pickerWheelSyncLockedUntil) return;

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

        commitPickerNumberFromWheel(index);
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
    commitPickerNumberFromWheel(index);
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
    if (value === 'winnings_available') return { label: t('client.ticket.status.winningsAvailable', 'Winnings available'), className: 'ticket-status-win' };
    if (value === 'winnings_claimed') return { label: t('client.ticket.status.winningsClaimed', 'Winnings claimed'), className: 'ticket-status-claimed' };
    if (value === 'expired_no_win') return { label: t('client.ticket.status.expired', 'Expired'), className: 'ticket-status-expired' };
    return { label: t('client.ticket.status.awaiting', 'Waiting for draw'), className: 'ticket-status-awaiting' };
  }

  function formatDrawStateLabel(state) {
    var value = String(state || '').toLowerCase();
    if (value === 'active') return t('client.draw.state.active', 'active');
    if (value === 'finished') return t('client.draw.state.finished', 'finished');
    if (value === 'upcoming') return t('client.draw.state.upcoming', 'upcoming');
    if (value === 'waiting') return t('client.currentDraw.waiting', 'waiting');
    return value || t('client.draw.state.unknown', 'unknown');
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
    title.textContent = t('client.ticket.titlePrefix', 'Ticket #') + ticket.id;

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

    var footer = document.createElement('div');
    footer.className = 'ticket-footer';
    footer.appendChild(meta);

    var winningAmount = Number(ticket && ticket.winningAmount || 0);
    if ((ticket && ticket.status === 'winnings_available') && Number.isFinite(winningAmount) && winningAmount > 0) {
      var claimBtn = document.createElement('button');
      claimBtn.type = 'button';
      claimBtn.className = 'ticket-claim-btn';
      claimBtn.textContent = t('client.ticket.claimPrefix', 'Claim') + ' ' + formatWinningsCurrency(winningAmount);
      claimBtn.addEventListener('click', function () {
        claimTicket(ticket.id, claimBtn);
      });
      footer.appendChild(claimBtn);
    } else if ((ticket && ticket.status === 'winnings_claimed') && Number.isFinite(winningAmount) && winningAmount > 0) {
      var claimedLabel = document.createElement('div');
      claimedLabel.className = 'ticket-claimed-label';
      claimedLabel.textContent = t('client.ticket.claimedPrefix', 'Claimed') + ' ' + formatWinningsCurrency(winningAmount);
      footer.appendChild(claimedLabel);
    }

    el.appendChild(footer);

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
    title.textContent = t('client.history.drawPrefix', 'Draw #') + group.drawId;

    var meta = document.createElement('div');
    meta.className = 'draw-header-meta';
    meta.textContent = draw ? formatUtc(draw.createdAtUtc) : t('client.history.createdLegacy', 'Created before current format');

    top.appendChild(title);
    top.appendChild(meta);
    header.appendChild(top);

    if (draw) {
      var info = document.createElement('div');
      info.className = 'draw-header-label';
      info.textContent = t('client.history.statePrefix', 'State:') + ' ' + formatDrawStateLabel(draw.state) + ' • ' + t('client.history.prizePoolPrefix', 'Prize pool:') + ' ' + formatPrizePool(draw.prizePool);
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
      empty.textContent = t('client.history.noTicketsForDraw', 'No tickets for this draw.');
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

  // Backwards-compatible shim: delegate to banner renderer
  function renderActiveDrawPicker(activeDraws) {
    try {
      renderActiveDrawBanners(activeDraws, getActiveTicketGroups(latestState));
    } catch (e) {
      // swallow errors to avoid breaking early initialization
      try { console.warn('renderActiveDrawPicker shim error', e); } catch (e) { }
    }
  }

  function renderActiveDrawBanners(activeDraws, activeTicketGroups) {
    if (!jackpotCardsContainerEl) {
      jackpotCardsContainerEl = document.getElementById('jackpotCardsContainer');
      if (!jackpotCardsContainerEl) return;
    }

    if (!featuredJackpotCardEl) {
      featuredJackpotCardEl = document.getElementById('featuredJackpotCard');
    }

    var draws = Array.isArray(activeDraws) ? activeDraws.filter(function (x) { return !!x; }) : [];
    var showMultiDrawBanners = draws.length > 1;

    if (featuredJackpotCardEl) {
      featuredJackpotCardEl.hidden = showMultiDrawBanners;
    }

    if (!showMultiDrawBanners) {
      jackpotCardsContainerEl.innerHTML = '';
      jackpotCardsContainerEl.hidden = true;
      return;
    }

    var ticketCountByDrawId = {};
    (activeTicketGroups || []).forEach(function (group) {
      if (!group || group.drawId == null) return;
      var count = Array.isArray(group.tickets) ? group.tickets.length : 0;
      ticketCountByDrawId[group.drawId] = count;
    });

    jackpotCardsContainerEl.innerHTML = '';
    jackpotCardsContainerEl.hidden = false;

    draws.forEach(function (draw) {
      var card = document.createElement('article');
      card.className = 'card jackpot-card duplicate-jackpot-card';
      if (selectedActiveDrawId === draw.id) {
        card.className += ' duplicate-jackpot-card-selected';
      }

      var top = document.createElement('div');
      top.className = 'jackpot-main';

      var rowTop = document.createElement('div');
      rowTop.className = 'jackpot-top';

      var badge = document.createElement('span');
      badge.className = 'state-badge ' + (draw.state === 'active' ? 'state-badge-active' : draw.state === 'finished' ? 'state-badge-finished' : 'state-badge-upcoming');
      badge.textContent = formatDrawStateLabel(draw.state);

      var stamp = document.createElement('span');
      stamp.className = 'jackpot-time';
      stamp.textContent = (draw.state === 'finished' ? t('client.currentDraw.concludedPrefix', 'Concluded ') : t('client.currentDraw.openedPrefix', 'Opened ')) + formatUtc(draw.createdAtUtc);

      rowTop.appendChild(badge);
      rowTop.appendChild(stamp);

      var headline = document.createElement('div');
      headline.className = 'jackpot-headline';

      var title = document.createElement('span');
      title.className = 'jackpot-title';
      title.textContent = t('client.history.drawPrefix', 'Draw #') + draw.id;

      var amount = document.createElement('span');
      amount.className = 'jackpot-amount';
      amount.textContent = formatJackpot(draw.prizePool);

      headline.appendChild(title);
      headline.appendChild(amount);

      var tiers = document.createElement('div');
      tiers.className = 'prize-tier-row';

      function appendTier(label, value) {
        var pill = document.createElement('div');
        pill.className = 'prize-tier-pill';
        var tierLabel = document.createElement('span');
        tierLabel.className = 'prize-tier-label';
        tierLabel.textContent = label;
        var tierValue = document.createElement('span');
        tierValue.className = 'prize-tier-value';
        tierValue.textContent = '$' + formatPrizeTierPool(value);
        pill.appendChild(tierLabel);
        pill.appendChild(tierValue);
        tiers.appendChild(pill);
      }

      appendTier('3/5', draw.prizePoolMatch3);
      appendTier('4/5', draw.prizePoolMatch4);
      appendTier('5/5', draw.prizePoolMatch5);

      top.appendChild(rowTop);
      top.appendChild(headline);
      top.appendChild(tiers);

      var buyRow = document.createElement('div');
      buyRow.className = 'jackpot-buy-row';

      var buyLeft = document.createElement('div');
      buyLeft.className = 'jackpot-buy-left';

      var costLabel = document.createElement('div');
      costLabel.className = 'jackpot-buy-price-label';
      costLabel.textContent = t('client.currentDraw.ticketCostPrefix', 'Ticket cost:');

      var costValue = document.createElement('div');
      costValue.className = 'jackpot-buy-price';
      costValue.textContent = formatCurrency(draw.ticketCost);

      buyLeft.appendChild(costLabel);
      buyLeft.appendChild(costValue);

      var buyRight = document.createElement('div');
      buyRight.className = 'jackpot-buy-right';

      var countText = document.createElement('div');
      countText.className = 'jackpot-buy-ticket-count';
      countText.textContent = t('client.tickets.title', 'My tickets') + ': ' + (ticketCountByDrawId[draw.id] || 0);

      var buyBtn = document.createElement('button');
      buyBtn.type = 'button';
      buyBtn.className = 'jackpot-buy-btn';
      buyBtn.textContent = t('client.button.purchase', 'Purchase ticket');
      buyBtn.disabled = draw.state !== 'active';
      buyBtn.title = draw.state === 'active' ? '' : t('client.currentDraw.activeOnlyTitle', 'Only the active draw accepts purchases.');

      buyBtn.addEventListener('click', function (event) {
        event.preventDefault();
        event.stopPropagation();

        if (draw.state !== 'active') {
          setPurchaseStatus(t('client.status.noActiveDraw', 'There is no active draw right now.'));
          return;
        }

        selectedActiveDrawId = draw.id;
        currentDisplayedDrawId = draw.id;
        setPurchaseStatus('');
        openTicketPicker();
        renderActiveDrawBanners(getActiveDraws(latestState), getActiveTicketGroups(latestState));
      });

      buyRight.appendChild(countText);
      buyRight.appendChild(buyBtn);

      buyRow.appendChild(buyLeft);
      buyRow.appendChild(buyRight);

      card.appendChild(top);
      card.appendChild(buyRow);

      card.addEventListener('click', function () {
        selectedActiveDrawId = draw.id;
        var selected = resolveSelectedDrawSnapshot(latestState);
        renderCurrentDraw(selected.draw, selected.tickets, selected.hasMultipleActiveDraws);
        renderActiveDrawBanners(getActiveDraws(latestState), getActiveTicketGroups(latestState));
      });

      jackpotCardsContainerEl.appendChild(card);
    });
  }

  function renderCurrentDraw(draw, currentTickets, hasMultipleActiveDraws) {
    // Re-bind key nodes if DOM was not ready at initial script evaluation.
    if (!currentDrawStateBadgeEl) currentDrawStateBadgeEl = document.getElementById('currentDrawStateBadge') || document.getElementById('jackpotGameStateBadge');
    if (!currentDrawPrizeTiersEl) currentDrawPrizeTiersEl = document.getElementById('currentDrawPrizeTiers');
    if (!currentDrawPrizePool3El) currentDrawPrizePool3El = document.getElementById('currentDrawPrizePool3');
    if (!currentDrawPrizePool4El) currentDrawPrizePool4El = document.getElementById('currentDrawPrizePool4');
    if (!currentDrawPrizePool5El) currentDrawPrizePool5El = document.getElementById('currentDrawPrizePool5');

    // legacy picker removed; banners will be rendered instead

    if (!draw) {
      if (jackpotChipEl) jackpotChipEl.textContent = t('client.currentDraw.waiting', 'waiting');
        if (currentDrawStateBadgeEl) {
          currentDrawStateBadgeEl.textContent = t('client.currentDraw.waiting', 'waiting');
          currentDrawStateBadgeEl.className = 'state-badge state-badge-muted';
        }
        if (currentDrawEmptyEl) currentDrawEmptyEl.hidden = false;
        if (currentDrawContentEl) currentDrawContentEl.hidden = true;
      if (currentDrawIdEl) currentDrawIdEl.textContent = t('client.jackpot.prizePoolTitle', 'Prize pool :');
      if (currentDrawPrizePoolEl) currentDrawPrizePoolEl.textContent = '$0.00';
      if (currentDrawCreatedAtEl) currentDrawCreatedAtEl.textContent = t('client.jackpot.endsIn', 'Ends in --:--:--');
      if (jackpotAmountEl) jackpotAmountEl.textContent = '$0';
      if (jackpotSubtitleEl) jackpotSubtitleEl.textContent = t('client.jackpot.subtitle', 'The next draw is coming soon. Get your tickets now.');
      if (currentDrawPrizeTiersEl) currentDrawPrizeTiersEl.setAttribute('hidden', 'hidden');
      if (currentDrawPrizePool3El) currentDrawPrizePool3El.textContent = '$0.00';
      if (currentDrawPrizePool4El) currentDrawPrizePool4El.textContent = '$0.00';
      if (currentDrawPrizePool5El) currentDrawPrizePool5El.textContent = '$0.00';
      if (purchaseBtn) {
        purchaseBtn.disabled = true;
        purchaseBtn.hidden = false;
        purchaseBtn.title = t('client.currentDraw.noActiveTitle', 'No active draw available.');
      }
      if (currentDrawSubtitleEl) currentDrawSubtitleEl.hidden = false;
      if (currentDrawTicketPriceRowEl) currentDrawTicketPriceRowEl.hidden = false;
      if (currentDrawTicketCostEl) currentDrawTicketCostEl.textContent = '$2.00';
      if (currentDrawPurchaseBlockEl) currentDrawPurchaseBlockEl.hidden = false;
      // legacy picker cleanup not required
      return;
    }

    var isFinishedDraw = draw.state === 'finished';

    if (currentDrawStateBadgeEl) {
      currentDrawStateBadgeEl.textContent = formatDrawStateLabel(draw.state);
      currentDrawStateBadgeEl.className = 'state-badge ' + (draw.state === 'active' ? 'state-badge-active' : draw.state === 'finished' ? 'state-badge-finished' : 'state-badge-upcoming');
    }
    if (jackpotChipEl) jackpotChipEl.textContent = formatDrawStateLabel(draw.state);
    if (currentDrawEmptyEl) currentDrawEmptyEl.hidden = true;
    if (currentDrawContentEl) currentDrawContentEl.hidden = false;

    if (currentDrawIdEl) currentDrawIdEl.textContent = t('client.jackpot.prizePoolTitle', 'Prize pool :');
    if (currentDrawPrizePoolEl) currentDrawPrizePoolEl.textContent = '$' + formatPrizePool(draw.prizePool);
    if (currentDrawCreatedAtEl) currentDrawCreatedAtEl.textContent = (draw.state === 'finished' ? t('client.currentDraw.concludedPrefix', 'Concluded ') : t('client.currentDraw.openedPrefix', 'Opened ')) + formatUtc(draw.createdAtUtc);
    if (jackpotAmountEl) jackpotAmountEl.textContent = formatJackpot(draw.prizePool);
    if (currentDrawPrizeTiersEl) currentDrawPrizeTiersEl.removeAttribute('hidden');
    if (currentDrawPrizePool3El) currentDrawPrizePool3El.textContent = '$' + formatPrizeTierPool(draw.prizePoolMatch3 || 0);
    if (currentDrawPrizePool4El) currentDrawPrizePool4El.textContent = '$' + formatPrizeTierPool(draw.prizePoolMatch4 || 0);
    if (currentDrawPrizePool5El) currentDrawPrizePool5El.textContent = '$' + formatPrizeTierPool(draw.prizePoolMatch5 || 0);
    if (currentDrawTicketCostEl) currentDrawTicketCostEl.textContent = formatCurrency(draw.ticketCost);

    // Update jackpot purchase price and track the displayed draw id
    try {
      currentDisplayedDrawId = draw ? draw.id : null;
      var jackpotPriceEl = document.getElementById('jackpotBuyPrice');
      if (jackpotPriceEl) jackpotPriceEl.textContent = formatCurrency(draw ? draw.ticketCost : 0);

      if (purchaseBtn) {
        // Ensure purchase button is visible and enabled only for active draws
        purchaseBtn.hidden = false;
        purchaseBtn.disabled = draw ? draw.state !== 'active' : true;
        purchaseBtn.title = draw && draw.state === 'active' ? '' : t('client.currentDraw.activeOnlyTitle', 'Only the active draw accepts purchases.');
      }
    } catch (e) {
      try { console.warn('renderCurrentDraw jackpot update error', e); } catch (e) { }
    }

    var hasWinningsAvailable = false;
    (currentTickets || []).forEach(function (ticket) {
      if (ticket && ticket.status === 'winnings_available') hasWinningsAvailable = true;
    });

    if (jackpotSubtitleEl) {
      if (draw.state === 'active') {
        jackpotSubtitleEl.textContent = t('client.currentDraw.liveSubtitle', 'The draw is live. Don\'t miss your chance to become a multi-millionaire!');
      } else if (draw.state === 'finished' && hasWinningsAvailable) {
        jackpotSubtitleEl.textContent = t('client.currentDraw.finishedWithWinnings', 'This draw is finished and your winnings are ready to claim.');
      } else if (draw.state === 'finished') {
        jackpotSubtitleEl.textContent = t('client.currentDraw.finishedNoWinnings', 'This draw is finished. Check your tickets against the result numbers.');
      } else {
        jackpotSubtitleEl.textContent = t('client.jackpot.subtitle', 'The next draw is coming soon. Get your tickets now.');
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
      purchaseBtn.title = draw.state === 'active' ? '' : t('client.currentDraw.activeOnlyTitle', 'Only the active draw accepts purchases.');
    }

    if (currentDrawSubtitleEl) currentDrawSubtitleEl.hidden = isFinishedDraw;
    if (currentDrawTicketPriceRowEl) currentDrawTicketPriceRowEl.hidden = isFinishedDraw;
    if (currentDrawPurchaseBlockEl) currentDrawPurchaseBlockEl.hidden = isFinishedDraw;
  }

  function buildMyTicketGroups(state) {
    var groups = [];
    var seenDraw = {};

    var activeTicketGroups = getActiveTicketGroups(state);
    activeTicketGroups.forEach(function (group) {
      if (!group || !group.draw || !Array.isArray(group.tickets) || group.tickets.length === 0) return;
      groups.push({ drawId: group.drawId, draw: group.draw, tickets: group.tickets.slice() });
      seenDraw[group.drawId] = true;
    });

    var currentDraw = state && state.currentDraw ? state.currentDraw : null;
    var currentTickets = state && state.currentTickets ? state.currentTickets : [];
    if (currentDraw && currentTickets.length > 0 && !seenDraw[currentDraw.id]) {
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

  function formatHistoryStatusLabel(status) {
    var normalized = String(status || '').toLowerCase();
    if (normalized === 'waiting_for_admin_approval') return t('client.history.status.waiting_for_admin_approval', 'Waiting for admin approval');
    if (normalized === 'rejected') return t('client.history.status.rejected', 'Rejected');
    if (normalized === 'paid') return t('client.history.status.paid', 'Paid');
    if (normalized === 'processing') return t('client.history.status.processing', 'Processing');
    if (normalized === 'expired') return t('client.history.status.expired', 'Expired');
    if (normalized === 'invalid') return t('client.history.status.invalid', 'Invalid');
    return normalized || t('client.history.status.unknown', 'Unknown');
  }

  function getHistoryStatusClass(status) {
    var normalized = String(status || '').toLowerCase();
    if (normalized === 'paid') return 'tx-status-paid';
    if (normalized === 'rejected') return 'tx-status-rejected';
    if (normalized === 'invalid') return 'tx-status-invalid';
    if (normalized === 'expired') return 'tx-status-expired';
    if (normalized === 'waiting_for_admin_approval') return 'tx-status-waiting';
    if (normalized === 'processing') return 'tx-status-processing';
    return '';
  }

  function formatHistoryKind(kind) {
    return String(kind || '').toLowerCase() === 'payout'
      ? t('client.history.kind.payout', 'Payout')
      : t('client.history.kind.topup', 'Top up');
  }

  function renderHistory() {
    if (!historyListEl || !historyEmptyEl) return;

    historyListEl.innerHTML = '';
    historyEmptyEl.hidden = historyEntries.length > 0;

    historyEntries.forEach(function (entry) {
      var row = document.createElement('div');
      row.className = 'tx-row';

      var top = document.createElement('div');
      top.className = 'tx-top';

      var kind = document.createElement('div');
      kind.className = 'tx-kind';
      kind.textContent = formatHistoryKind(entry.kind);

      var status = document.createElement('div');
      status.className = 'tx-status ' + getHistoryStatusClass(entry.status);
      status.textContent = formatHistoryStatusLabel(entry.status);

      top.appendChild(kind);
      top.appendChild(status);

      var amount = document.createElement('div');
      amount.className = 'tx-amount';
      var sign = String(entry.kind || '').toLowerCase() === 'payout' ? '-' : '+';
      amount.textContent = sign + formatCurrency(entry.amount || 0);

      var sub = document.createElement('div');
      sub.className = 'tx-sub';
      var dateText = formatUtc(entry.createdAtUtc);
      sub.textContent = dateText + (entry.externalId ? (' • ' + entry.externalId) : '');

      row.appendChild(top);
      row.appendChild(amount);
      row.appendChild(sub);
      historyListEl.appendChild(row);
    });
  }

  function setProfileScreen(name) {
    var nextScreen = String(name || 'home').toLowerCase();
    if (nextScreen !== 'home' && nextScreen !== 'deposit' && nextScreen !== 'invite' && nextScreen !== 'withdraw' && nextScreen !== 'history') {
      nextScreen = 'home';
    }

    activeProfileScreen = nextScreen;

    if (profileHomeScreenEl) profileHomeScreenEl.hidden = nextScreen !== 'home';
    if (profileDepositScreenEl) profileDepositScreenEl.hidden = nextScreen !== 'deposit';
    if (profileInviteScreenEl) profileInviteScreenEl.hidden = nextScreen !== 'invite';
    if (profileWithdrawScreenEl) profileWithdrawScreenEl.hidden = nextScreen !== 'withdraw';
    if (historyScreenEl) historyScreenEl.hidden = nextScreen !== 'history';

    if (nextScreen === 'history') {
      loadHistory();
    }
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

    // Any footer tab click resets profile to its home menu.
    setProfileScreen('home');
  }

  function openTicketPicker() {
    if (!ticketPickerSheetEl) return;

    if (!ticketPickerReady) {
      ticketPickerPendingOpen = true;
      setTicketPickerStatus(t('client.picker.preparing', 'Preparing numbers...'));
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

      if (!pickerHasAnimatedOpen) {
        pickerApplyingSeed = true;
        lockPickerWheelSync(900);

        for (var i = 0; i < pickerWheels.length; i++) {
          setWheelStartNearSeed(i);
        }

        pickerHasAnimatedOpen = true;

        pickerOpenAnimationTimer = setTimeout(function () {
          pickerOpenAnimationTimer = null;
          startPickerOpenAnimation();
        }, 72);

        return;
      }

      pickerApplyingSeed = false;
      lockPickerWheelSync(200);
      restorePickerWheelPositions();
      updatePickerUi();
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

    var selected = resolveSelectedDrawSnapshot(latestState);
    var selectedDrawId = selected.draw && selected.draw.state === 'active'
      ? selected.draw.id
      : null;

    var validation = validatePickerSelection();
    if (!validation.ok) {
      setTicketPickerStatus(validation.message);
      showCenterPopup(validation.message);
      return;
    }

    if (confirmTicketNumbersBtn) confirmTicketNumbersBtn.disabled = true;
    if (purchaseBtn) purchaseBtn.disabled = true;
    setTicketPickerStatus(t('client.picker.submitting', 'Submitting ticket...'));

    postJson('/api/tickets/purchase', {
      initData: initData || '',
      numbers: pickerNumbers.slice(0, LOTTO_NUMBERS_COUNT),
      drawId: selectedDrawId
    }, null)
      .then(function (res) {
        if (res && res.ok && res.ticket) {
          highlightTicketId = res.ticket.id;
          setPurchaseStatus(t('client.status.ticketPurchased', 'Ticket purchased.'));
          closeTicketPicker();
          randomizePickerNumbers();
          pickerHasAnimatedOpen = false;
          if (ticketPickerReady) updatePickerUi();
          return refreshState();
        }

        setTicketPickerStatus(t('client.status.purchaseFailed', 'Purchase failed.'));
      })
      .catch(function (err) {
        setTicketPickerStatus(err.message);
        if (shouldShowInvalidTicketPopup(err && err.message)) {
          showCenterPopup(err.message);
        }
      })
      .finally(function () {
        updatePickerUi();
        if (purchaseBtn) {
          var snapshot = resolveSelectedDrawSnapshot(latestState);
          purchaseBtn.disabled = !(snapshot.draw && snapshot.draw.state === 'active');
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
        return r.text().then(function (rawBody) {
          var body = null;
          if (rawBody) {
            try {
              body = JSON.parse(rawBody);
            } catch (e) {
              body = null;
            }
          }

          var msg = '';
          if (body && typeof body === 'object') {
            msg = String(
              body.error
              || body.detail
              || body.title
              || body.message
              || '').trim();
          }

          if (!msg) {
            msg = (rawBody || '').trim();
          }

          if (!msg) {
            msg = 'HTTP ' + r.status;
          }

          var traceId = body && typeof body === 'object'
            ? (body.traceId || (body.debug && body.debug.traceId) || null)
            : null;
          var failureStage = body && typeof body === 'object' && body.debug
            ? (body.debug.failureStage || null)
            : null;
          if (failureStage) {
            msg += ' [stage: ' + failureStage + ']';
          }
          if (traceId) {
            msg += ' [traceId: ' + traceId + ']';
          }

          var err = new Error(msg);
          err.status = r.status;
          err.body = body;
          err.rawBody = rawBody;
          throw err;
        });
      }

      return r.json();
    });
  }

  function handleAuthFailure(err) {
    var status = err && err.status;
    markAppReady();
    if (status === 401) {
      setTimelineStatus(t('client.status.authOpenFromTelegram', 'Authentication failed. Open this app from Telegram, or use the local debug profile in Development.'));
      setPurchaseStatus(t('client.status.authenticationFailed', 'Authentication failed.'));
      return;
    }

    setPurchaseStatus(err && err.message ? err.message : t('client.status.authenticationFailed', 'Authentication failed.'));
  }

  function computeStateSig(state) {
    try {
      return JSON.stringify({
        currentDraw: state && state.currentDraw ? {
          id: state.currentDraw.id,
          prizePool: state.currentDraw.prizePool,
          prizePoolMatch3: state.currentDraw.prizePoolMatch3,
          prizePoolMatch4: state.currentDraw.prizePoolMatch4,
          prizePoolMatch5: state.currentDraw.prizePoolMatch5,
          ticketCost: state.currentDraw.ticketCost,
          state: state.currentDraw.state,
          numbers: state.currentDraw.numbers || null,
          createdAtUtc: state.currentDraw.createdAtUtc || null
        } : null,
        activeDraws: (state && state.activeDraws || []).map(function (draw) {
          return {
            id: draw.id,
            prizePool: draw.prizePool,
            prizePoolMatch3: draw.prizePoolMatch3,
            prizePoolMatch4: draw.prizePoolMatch4,
            prizePoolMatch5: draw.prizePoolMatch5,
            ticketCost: draw.ticketCost,
            state: draw.state,
            numbers: draw.numbers || null,
            createdAtUtc: draw.createdAtUtc || null
          };
        }),
        activeTicketGroups: (state && state.activeTicketGroups || []).map(function (group) {
          return {
            drawId: group.drawId,
            tickets: (group.tickets || []).map(function (ticket) {
              return {
                id: ticket.id,
                drawId: ticket.drawId,
                numbers: ticket.numbers || null,
                status: ticket.status || null,
                purchasedAtUtc: ticket.purchasedAtUtc || null,
                winningAmount: Number(ticket.winningAmount || 0)
              };
            })
          };
        }),
        currentTickets: (state && state.currentTickets || []).map(function (ticket) {
          return {
            id: ticket.id,
            drawId: ticket.drawId,
            numbers: ticket.numbers || null,
            status: ticket.status || null,
            purchasedAtUtc: ticket.purchasedAtUtc || null,
            winningAmount: Number(ticket.winningAmount || 0)
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
                purchasedAtUtc: ticket.purchasedAtUtc || null,
                winningAmount: Number(ticket.winningAmount || 0)
              };
            })
          };
        })
        ,
        balance: state && Number.isFinite(Number(state.balance)) ? Number(state.balance) : 0
      });
    } catch (e) {
      return String(Math.random());
    }
  }

  function applyState(state) {
    latestState = state || { balance: 0, currentDraw: null, activeDraws: [], activeTicketGroups: [], currentTickets: [], history: [] };

    var activeDraws = getActiveDraws(latestState);
    if (activeDraws.length === 0) {
      selectedActiveDrawId = null;
    } else {
      var selectedExists = selectedActiveDrawId != null && activeDraws.some(function (x) { return x && x.id === selectedActiveDrawId; });
      if (!selectedExists) {
        selectedActiveDrawId = activeDraws[0].id;
      }
    }

    renderBalance(latestState.balance);
    setTicketsWinningPin(countWinningTickets(latestState));

    var selected = resolveSelectedDrawSnapshot(latestState);
    renderCurrentDraw(selected.draw, selected.tickets, selected.hasMultipleActiveDraws);
    renderActiveDrawBanners(activeDraws, getActiveTicketGroups(latestState));
    renderMyTickets(latestState);

    if (clientIsLocalDebug && !autoOpenedTicketsTab) {
      var ticketGroups = buildMyTicketGroups(latestState);
      if (ticketGroups.length > 0 && activeTabName !== 'tickets') {
        autoOpenedTicketsTab = true;
        setActiveTab('tickets');
      }
    }
  }

  function countWinningTickets(state) {
    var count = 0;
    var countedTicketIds = {};

    function includeTickets(items) {
      (items || []).forEach(function (ticket) {
        if (!ticket || ticket.status !== 'winnings_available') return;

        var ticketId = ticket.id;
        if (ticketId != null && countedTicketIds[ticketId]) return;

        if (ticketId != null) countedTicketIds[ticketId] = true;
        count++;
      });
    }

    var currentTickets = state && state.currentTickets ? state.currentTickets : [];
    includeTickets(currentTickets);

    var activeTicketGroups = getActiveTicketGroups(state);
    activeTicketGroups.forEach(function (group) {
      includeTickets(group && group.tickets ? group.tickets : []);
    });

    var history = state && state.history ? state.history : [];
    history.forEach(function (group) {
      includeTickets(group && group.tickets ? group.tickets : []);
    });

    return count;
  }

  function claimTicket(ticketId, buttonEl) {
    if (!initData) return;

    if (buttonEl) {
      buttonEl.disabled = true;
      buttonEl.classList.add('is-loading');
    }
    setPurchaseStatus(t('client.ticket.claiming', 'Claiming winnings...'));

    postJson('/api/tickets/claim', { initData: initData || '', ticketId: ticketId }, null)
      .then(function (res) {
        if (!(res && res.ok)) {
          setPurchaseStatus(t('client.ticket.claimFailed', 'Claim failed.'));
          return;
        }

        latestState.balance = Number(res.balance || 0);
        renderBalance(latestState.balance);
        setPurchaseStatus(t('client.ticket.claimedPrefix', 'Claimed') + ' ' + formatWinningsCurrency(res.amount || 0) + '.');
        return refreshState();
      })
      .catch(function (err) {
        setPurchaseStatus(err.message || t('client.ticket.claimFailed', 'Claim failed.'));
      })
      .finally(function () {
        if (buttonEl) {
          buttonEl.disabled = false;
          try { buttonEl.classList.remove('is-loading'); } catch (e) { }
        }
      });
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
          (res.state.activeTicketGroups || []).forEach(function (group) {
            (group && group.tickets ? group.tickets : []).forEach(function (ticket) {
              if (ticket && ticket.id === highlightTicketId) found = true;
            });
          });
          if (!found) highlightTicketId = null;
        }

        var sig = computeStateSig(res.state);
        if (sig !== lastStateSig) {
          lastStateSig = sig;
          applyState(res.state);
        }

        markAppReady();

        setTimelineStatus('');
      })
      .catch(function (err) {
        console.warn('Failed to load app state', err);
        setTimelineStatus(err.message);
      });
  }

  function loadWalletAddress() {
    if (!initData) return Promise.resolve(null);

    return postJson('/api/wallet/address/get', { initData: initData || '' }, null)
      .then(function (res) {
        if (!(res && res.ok)) return null;

        var address = String(res.address || '').trim();
        if (walletAddressInputEl) walletAddressInputEl.value = address;
        if (withdrawNumberInputEl && !String(withdrawNumberInputEl.value || '').trim()) {
          withdrawNumberInputEl.value = address;
        }
        return address;
      })
      .catch(function () {
        return null;
      });
  }

  function saveWalletAddress() {
    if (!initData) return;

    var address = walletAddressInputEl ? String(walletAddressInputEl.value || '').trim() : '';
    if (!address) {
      setWalletAddressStatus(t('client.wallet.enterAddressFirst', 'Enter wallet address first.'));
      return;
    }

    if (saveWalletAddressBtn) saveWalletAddressBtn.disabled = true;
    setWalletAddressStatus(t('client.wallet.savingAddress', 'Saving wallet address...'));

    postJson('/api/wallet/address/save', { initData: initData || '', address: address }, null)
      .then(function (res) {
        if (!(res && res.ok)) {
          setWalletAddressStatus(t('client.wallet.saveFailed', 'Failed to save wallet address.'));
          return;
        }

        var normalized = String(res.address || address);
        if (walletAddressInputEl) walletAddressInputEl.value = normalized;
        if (withdrawNumberInputEl) withdrawNumberInputEl.value = normalized;
        setWalletAddressStatus(t('client.wallet.saved', 'Wallet address saved.'));
      })
      .catch(function (err) {
        setWalletAddressStatus(err.message || t('client.wallet.saveFailed', 'Failed to save wallet address.'));
      })
      .finally(function () {
        if (saveWalletAddressBtn) saveWalletAddressBtn.disabled = false;
      });
  }

  function loadHistory() {
    if (!initData) return Promise.resolve(null);

    setHistoryStatus(t('client.status.loadingHistory', 'Loading transaction history...'));
    return postJson('/api/wallet/history', { initData: initData || '', limit: 100 }, null)
      .then(function (res) {
        if (!(res && res.ok && Array.isArray(res.entries))) {
          historyEntries = [];
          renderHistory();
          setHistoryStatus('');
          return;
        }

        historyEntries = res.entries.slice();
        renderHistory();
        setHistoryStatus('');
      })
      .catch(function (err) {
        setHistoryStatus(err.message || t('client.history.loadFailed', 'Failed to load transaction history.'));
      });
  }

  function pollDepositStatus(depositId, attemptsLeft) {
    if (!initData || !depositId || attemptsLeft <= 0) {
      return Promise.resolve(null);
    }

    return postJson('/api/payments/deposits/status', {
      initData: initData || '',
      depositId: depositId
    }, null)
      .then(function (res) {
        if (!(res && res.ok && res.deposit)) {
          return null;
        }

        var deposit = res.deposit;
        var status = String(deposit.status || '').toLowerCase();
        if (status === 'credited') {
          setTopUpStatus(t('client.topup.creditedPrefix', 'Deposit credited: +') + formatCurrency(deposit.amount || 0) + '.');
          return refreshState()
            .then(function () { return loadHistory(); })
            .then(function () { return loadReferralProfile(); });
        }

        if (status === 'expired' || status === 'invalid') {
          setTopUpStatus(t('client.topup.statusPrefix', 'Deposit ') + formatHistoryStatusLabel(status) + '. ' + t('client.topup.createNew', 'Please create a new one.'));
          return null;
        }

        return new Promise(function (resolve) {
          setTimeout(function () {
            pollDepositStatus(depositId, attemptsLeft - 1).then(resolve);
          }, 4000);
        });
      })
      .catch(function () {
        return null;
      });
  }

  function openCheckoutLink(url) {
    var checkoutUrl = String(url || '').trim();
    if (!checkoutUrl) return;

    // iOS Telegram WebView can block async window.open; prefer Telegram's native openLink API when available.
    try {
      if (window.Telegram && Telegram.WebApp && typeof Telegram.WebApp.openLink === 'function') {
        Telegram.WebApp.openLink(checkoutUrl);
        return;
      }
    } catch (e) {
    }

    try {
      var opened = window.open(checkoutUrl, '_blank', 'noopener');
      if (opened) return;
    } catch (e) {
    }

    try {
      window.location.assign(checkoutUrl);
    } catch (e) {
    }
  }

  function topUpBalance() {
    if (!initData) return;

    var topUpAmount = topUpAmountInputEl ? Number(topUpAmountInputEl.value) : NaN;
    if (!Number.isFinite(topUpAmount) || topUpAmount <= 0) {
      setTopUpStatus(t('client.topup.enterValidAmount', 'Enter a valid top up amount.'));
      return;
    }

    if (topUpBtn) topUpBtn.disabled = true;
    setTopUpStatus(t('client.topup.creatingInvoice', 'Creating crypto invoice...'));
    setWithdrawStatus('');

    postJson('/api/payments/deposits/create', { initData: initData || '', amount: topUpAmount, currency: 'USD' }, null)
      .then(function (res) {
        if (!(res && res.ok && res.deposit)) {
          setTopUpStatus(t('client.topup.createFailed', 'Failed to create deposit invoice.'));
          return;
        }

        var deposit = res.deposit;
        if (deposit.checkoutLink) {
          openCheckoutLink(deposit.checkoutLink);
        }

        setTopUpStatus(t('client.topup.created', 'Invoice created. Complete payment in BTCPay; status updates every 4s.'));
        return pollDepositStatus(deposit.id, 45);
      })
      .catch(function (err) {
        setTopUpStatus(err.message || t('client.topup.createFailed', 'Failed to create deposit invoice.'));
      })
      .finally(function () {
        if (topUpBtn) topUpBtn.disabled = false;
      });
  }

  function loadReferralProfile() {
    if (!initData) return Promise.resolve(null);

    return postJson('/api/referrals/me', { initData: initData || '' }, null)
      .then(function (res) {
        if (!(res && res.ok && res.profile)) return null;

        var profile = res.profile;
        referralIsBound = !!profile.isBound;
        referralInviteCode = String(profile.inviteCode || '').trim();
        referralInviteLink = String(profile.inviteLink || '').trim();

        if (referralCodeTextEl) {
          referralCodeTextEl.textContent = referralInviteCode || '-';
        }

        if (profile.isBound) {
          setReferralStatus(t('client.referral.bound', 'Referral is already linked for this account.'));
        } else {
          setReferralStatus('');
        }

        return profile;
      })
      .catch(function () {
        return null;
      });
  }

  function bindReferralCodeFromQuery() {
    var code = String(referralCodeFromQuery || '').trim();
    if (!initData || !code) return Promise.resolve(null);

    referralCodeFromQuery = '';

    return postJson('/api/referrals/bind', { initData: initData || '', inviteCode: code }, null)
      .then(function (res) {
        if (res && res.ok) {
          var bonusAmount = Number(res.bonusAmount || 0);
          var minDepositAmount = Number(res.minDepositAmount || 0);
          if (res.rewardsEnabled && bonusAmount > 0) {
            setReferralStatus(
              t('client.referral.bindSuccessWithAmountPrefix', 'Promo applied. Bonus: ')
              + formatCurrency(bonusAmount)
              + ' '
              + t('client.referral.bindSuccessWithAmountSuffix', 'after your first successful crypto deposit of at least ')
              + formatCurrency(minDepositAmount)
              + '.');
          } else {
            setReferralStatus(t('client.referral.bindSuccess', 'Invite code linked. You will receive a bonus on your first successful crypto deposit.'));
          }
          return loadReferralProfile();
        }

        return null;
      })
      .catch(function (err) {
        var message = err && err.message ? String(err.message) : '';
        if (message.indexOf('Referral is already bound for this account.') >= 0) {
          return loadReferralProfile();
        }

        setReferralStatus(message || t('client.referral.bindFailed', 'Failed to link invite code.'));
        return null;
      });
  }

  function applyPromoCode() {
    if (!initData) {
      setPromoStatus(t('client.promo.notReady', 'App is still initializing. Please wait a moment and try again.'));
      return;
    }

    if (referralIsBound) {
      // Keep UX hint, but continue and call backend check for diagnostics/log visibility.
      setPromoStatus(t('client.referral.bound', 'Referral is already linked for this account.'));
    }

    var code = promoCodeInputEl ? String(promoCodeInputEl.value || '').trim() : '';
    if (!code) {
      setPromoStatus(t('client.promo.enterCode', 'Enter promo code first.'));
      return;
    }

    if (applyPromoBtn) applyPromoBtn.disabled = true;
    setPromoStatus(t('client.promo.checking', 'Checking promo code...'));

    postJson('/api/referrals/check', { initData: initData || '', inviteCode: code }, null)
      .then(function (checkRes) {
        if (!(checkRes && checkRes.ok)) {
          setPromoStatus(t('client.promo.applyFailed', 'Promo code is invalid or cannot be applied.'));
          return null;
        }

        return postJson('/api/referrals/bind', { initData: initData || '', inviteCode: code }, null);
      })
      .then(function (res) {
        if (!res) return;
        if (!(res && res.ok)) {
          setPromoStatus(t('client.promo.applyFailed', 'Promo code is invalid or cannot be applied.'));
          return;
        }

        var bonusAmount = Number(res.bonusAmount || 0);
        var minDepositAmount = Number(res.minDepositAmount || 0);
        if (res.rewardsEnabled && bonusAmount > 0) {
          setPromoStatus(
            t('client.promo.appliedWithAmountPrefix', 'Promo applied. Your bonus: ')
            + formatCurrency(bonusAmount)
            + '. '
            + t('client.promo.appliedWithAmountSuffix', 'It will be credited after your first successful crypto top up of at least ')
            + formatCurrency(minDepositAmount)
            + '.');
        } else {
          setPromoStatus(t('client.promo.applied', 'Promo code applied.'));
        }

        if (promoCodeInputEl) promoCodeInputEl.value = '';
        return loadReferralProfile();
      })
      .catch(function (err) {
        setPromoStatus(err && err.message ? err.message : t('client.promo.applyFailed', 'Promo code is invalid or cannot be applied.'));
      })
      .finally(function () {
        if (applyPromoBtn) applyPromoBtn.disabled = false;
      });
  }

  function copyReferralLink() {
    var value = String(referralInviteLink || '').trim();
    if (!value) {
      setReferralStatus(t('client.referral.noLink', 'Referral link is not ready yet.'));
      return;
    }

    if (navigator && navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(value)
        .then(function () {
          setReferralStatus(t('client.referral.copySuccess', 'Invite link copied.'));
        })
        .catch(function () {
          setReferralStatus(t('client.referral.copyFallback', 'Copy failed. Share this code: ') + (referralInviteCode || value));
        });
      return;
    }

    setReferralStatus(t('client.referral.copyFallback', 'Copy failed. Share this code: ') + (referralInviteCode || value));
  }

  function withdrawBalance() {
    if (!initData) return;

    var amount = withdrawAmountInputEl ? Number(withdrawAmountInputEl.value) : NaN;
    var number = withdrawNumberInputEl ? String(withdrawNumberInputEl.value || '').trim() : '';

    if (!Number.isFinite(amount) || amount <= 0) {
      setWithdrawStatus(t('client.withdraw.enterValidAmount', 'Enter a valid withdrawal amount.'));
      return;
    }

    if (!number && !(walletAddressInputEl && String(walletAddressInputEl.value || '').trim())) {
      setWithdrawStatus(t('client.withdraw.enterAddressOrSave', 'Enter wallet address or save one first.'));
      return;
    }

    if (withdrawBtn) withdrawBtn.disabled = true;
    setTopUpStatus('');
    setWithdrawStatus(t('client.withdraw.submitting', 'Submitting withdrawal request...'));

    postJson('/api/wallet/withdraw', {
      initData: initData || '',
      amount: amount,
      number: number
    }, null)
      .then(function (res) {
        if (!(res && res.ok)) {
          setWithdrawStatus(t('client.withdraw.failed', 'Withdrawal request failed.'));
          return;
        }

        latestState.balance = Number(res.balance || 0);
        renderBalance(latestState.balance);
        if (res.walletAddress && walletAddressInputEl) walletAddressInputEl.value = String(res.walletAddress);
        if (withdrawAmountInputEl) withdrawAmountInputEl.value = '';
        setWithdrawStatus(
          t('client.withdraw.requestPrefix', 'Withdrawal request #')
          + res.requestId
          + ' '
          + t('client.withdraw.submittedFor', 'submitted for ')
          + formatCurrency(res.amount)
          + '. '
          + t('client.withdraw.waitingApproval', 'Waiting for admin approval.'));
        loadHistory();
      })
      .catch(function (err) {
        setWithdrawStatus(err.message || t('client.withdraw.failed', 'Withdrawal request failed.'));
      })
      .finally(function () {
        if (withdrawBtn) withdrawBtn.disabled = false;
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
    var selected = resolveSelectedDrawSnapshot(latestState);
    if (!selected.draw || selected.draw.state !== 'active') {
      setPurchaseStatus(t('client.status.noActiveDraw', 'There is no active draw right now.'));
      return;
    }

    setPurchaseStatus('');
    // Ensure purchases from jackpot card target the featured/current draw
    if (currentDisplayedDrawId != null) selectedActiveDrawId = currentDisplayedDrawId;
    openTicketPicker();
  }

  if (purchaseBtn) purchaseBtn.addEventListener('click', purchaseTicket);
  if (lotteryTabBtn) lotteryTabBtn.addEventListener('click', function () { setActiveTab('lottery'); });
  if (ticketsTabBtn) ticketsTabBtn.addEventListener('click', function () { setActiveTab('tickets'); });
  if (profileTabBtn) profileTabBtn.addEventListener('click', function () { setActiveTab('profile'); });
  if (closeTicketPickerBtn) closeTicketPickerBtn.addEventListener('click', closeTicketPicker);
  if (ticketPickerBackdropEl) ticketPickerBackdropEl.addEventListener('click', closeTicketPicker);
  if (confirmTicketNumbersBtn) confirmTicketNumbersBtn.addEventListener('click', confirmSelectedTicketNumbers);
  if (centerPopupConfirmBtn) centerPopupConfirmBtn.addEventListener('click', hideCenterPopup);
  if (centerPopupBackdropEl) centerPopupBackdropEl.addEventListener('click', hideCenterPopup);
  // active draw selection handled via per-banner buy buttons
  if (topUpBtn) topUpBtn.addEventListener('click', topUpBalance);
  if (applyPromoBtn) applyPromoBtn.addEventListener('click', applyPromoCode);
  if (promoCodeInputEl) {
    promoCodeInputEl.addEventListener('keydown', function (event) {
      if (event && event.key === 'Enter') {
        event.preventDefault();
        applyPromoCode();
      }
    });
  }
  if (copyReferralLinkBtn) copyReferralLinkBtn.addEventListener('click', copyReferralLink);
  if (withdrawBtn) withdrawBtn.addEventListener('click', withdrawBalance);
  if (saveWalletAddressBtn) saveWalletAddressBtn.addEventListener('click', saveWalletAddress);
  if (openDepositScreenBtn) openDepositScreenBtn.addEventListener('click', function () { setProfileScreen('deposit'); });
  if (openInviteScreenBtn) openInviteScreenBtn.addEventListener('click', function () { setProfileScreen('invite'); });
  if (openWithdrawScreenBtn) openWithdrawScreenBtn.addEventListener('click', function () { setProfileScreen('withdraw'); });
  if (openHistoryScreenBtn) openHistoryScreenBtn.addEventListener('click', function () { setProfileScreen('history'); });
  if (backFromDepositBtn) backFromDepositBtn.addEventListener('click', function () { setProfileScreen('home'); });
  if (backFromInviteBtn) backFromInviteBtn.addEventListener('click', function () { setProfileScreen('home'); });
  if (backFromWithdrawBtn) backFromWithdrawBtn.addEventListener('click', function () { setProfileScreen('home'); });
  if (closeHistoryBtn) closeHistoryBtn.addEventListener('click', function () { setProfileScreen('home'); });

  var search = '';
  try {
    search = window.location && window.location.search ? window.location.search : '';
  } catch (e) {
    search = '';
  }

  var query = new URLSearchParams(search || '');
  var forceLocalDebug = query.get('debug') === '1' || query.get('mode') === 'local-debug';
  referralCodeFromQuery = String(query.get('ref') || '').trim();

  loadLocaleFromCache();

  randomizePickerNumbers();
  preloadTicketPicker();
  setActiveTab('lottery');
  setProfileScreen('home');
  renderCurrentDraw(null, [], false);
  renderMyTickets({ balance: 0, currentDraw: null, activeDraws: [], activeTicketGroups: [], currentTickets: [], history: [] });
  renderHistory();
  renderBalance(0);
  if (topUpAmountInputEl && !String(topUpAmountInputEl.value || '').trim()) {
    topUpAmountInputEl.value = '10';
  }

  var hasTelegramInitData = false;
  try {
    hasTelegramInitData = !!(window.Telegram && Telegram.WebApp && Telegram.WebApp.initData && Telegram.WebApp.initData.length > 0);
  } catch (e) {
    hasTelegramInitData = false;
  }

  if (forceLocalDebug || !hasTelegramInitData) {
    clientIsLocalDebug = true;
    debugModeBadgeReason = forceLocalDebug ? 'forced-local' : 'missing-init-data';
    setDebugModeBadge(getDebugModeBadgeText(debugModeBadgeReason));

    initData = 'local-debug';

    // watchdog: ensure the app gets out of loading overlay if the local-debug bootstrap chain stalls
    try { if (localDebugWatchdog) clearTimeout(localDebugWatchdog); } catch (e) { }
    localDebugWatchdog = setTimeout(function () {
      try { console.warn('local-debug watchdog: marking app ready due to timeout'); } catch (e) { }
      try { markAppReady(); } catch (e) { }
    }, 5000);

    loadRemoteLocale(initData)
      .then(function () { return postJson('/api/auth/telegram', { initData: initData }); })
      .then(function () { return loadReferralProfile(); })
      .then(function () { return bindReferralCodeFromQuery(); })
      .then(function () { return refreshState(); })
      .then(function () { return loadWalletAddress(); })
      .then(function () { return loadHistory(); })
      .then(function () {
        startPolling();
        // clear watchdog when startup finished successfully
        try { if (localDebugWatchdog) { clearTimeout(localDebugWatchdog); localDebugWatchdog = null; } } catch (e) { }
      })
      .catch(function (err) {
        console.warn('Failed local debug auth', err);
        try { if (localDebugWatchdog) { clearTimeout(localDebugWatchdog); localDebugWatchdog = null; } } catch (e) { }
        handleAuthFailure(err);
      });

    return;
  }

  setDebugModeBadge('');
  debugModeBadgeReason = '';
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
    loadRemoteLocale(initData)
      .then(function () { return postJson('/api/auth/telegram', { initData: initData }); })
      .then(function () { return loadReferralProfile(); })
      .then(function () { return bindReferralCodeFromQuery(); })
      .then(function () { return refreshState(); })
      .then(function () { return loadWalletAddress(); })
      .then(function () { return loadHistory(); })
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
