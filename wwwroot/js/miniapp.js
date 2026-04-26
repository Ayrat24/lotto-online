(function () {
  var purchaseBtn = document.getElementById('purchaseTicketBtn');
  var purchaseStatusEl = document.getElementById('purchaseStatus');
  var timelineStatusEl = document.getElementById('timelineStatus');
  var topUpBtn = document.getElementById('topUpBtn');
  var topUpStatusEl = document.getElementById('topUpStatus');
  var topUpTonStatusEl = document.getElementById('topUpTonStatus');
  var topUpAmountInputEl = document.getElementById('topUpAmountInput');
  var paymentSystemsHintEl = document.getElementById('paymentSystemsHint');
  var paymentSystemsListEl = document.getElementById('paymentSystemsList');
  var depositDetailsCardEl = document.getElementById('depositDetailsCard');
  var depositDetailsAmountEl = document.getElementById('depositDetailsAmount');
  var depositDetailsAddressEl = document.getElementById('depositDetailsAddress');
  var depositDetailsMemoEl = document.getElementById('depositDetailsMemo');
  var depositDetailsTxEl = document.getElementById('depositDetailsTx');
  var openDepositWalletBtn = document.getElementById('openDepositWalletBtn');
  var openDepositAltBtn = document.getElementById('openDepositAltBtn');
  var copyDepositAddressBtn = document.getElementById('copyDepositAddressBtn');
  var copyDepositMemoBtn = document.getElementById('copyDepositMemoBtn');
  var copyDepositTxBtn = document.getElementById('copyDepositTxBtn');
  var checkDepositStatusBtn = document.getElementById('checkDepositStatusBtn');
  var tonConnectPanelEl = document.getElementById('tonConnectPanel');
  var tonConnectStatusEl = document.getElementById('tonConnectStatus');
  var tonConnectConnectBtn = document.getElementById('tonConnectConnectBtn');
  var tonConnectDisconnectBtn = document.getElementById('tonConnectDisconnectBtn');
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
  var backFromTonDepositBtn = document.getElementById('backFromTonDepositBtn');
  var backFromInviteBtn = document.getElementById('backFromInviteBtn');
  var backFromWithdrawBtn = document.getElementById('backFromWithdrawBtn');
  var closeHistoryBtn = document.getElementById('closeHistoryBtn');
  var profileHomeScreenEl = document.getElementById('profileHomeScreen');
  var profileDepositScreenEl = document.getElementById('profileDepositScreen');
  var profileTonDepositScreenEl = document.getElementById('profileTonDepositScreen');
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
  var winnersTabBtn = document.getElementById('winnersTabBtn');
  var profileTabBtn = document.getElementById('profileTabBtn');
  var lotteryTabPanel = document.getElementById('lotteryTabPanel');
  var ticketsTabPanel = document.getElementById('ticketsTabPanel');
  var winnersTabPanel = document.getElementById('winnersTabPanel');
  var profileTabPanel = document.getElementById('profileTabPanel');
  var winnersStatusEl = document.getElementById('winnersStatus');
  var winnersEmptyEl = document.getElementById('winnersEmpty');
  var winnersListEl = document.getElementById('winnersList');

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
  var newsBannerSectionEl = document.getElementById('newsBannerSection');
  var newsBannerCarouselEl = document.getElementById('newsBannerCarousel');
  var newsBannerTrackEl = document.getElementById('newsBannerTrack');
  var newsBannerDotsEl = document.getElementById('newsBannerDots');
  var currentDrawNumbersWrapEl = document.getElementById('currentDrawNumbersWrap');
  var currentDrawNumbersEl = document.getElementById('currentDrawNumbers');
  var currentDrawTicketPriceRowEl = document.getElementById('currentDrawTicketPriceRow');
  var currentDrawTicketCostEl = document.getElementById('currentDrawTicketCost');
  var currentDrawPurchaseBlockEl = document.getElementById('currentDrawPurchaseBlock');
  var featuredJackpotCardEl = document.getElementById('featuredJackpotCard');
  var jackpotCardsContainerEl = document.getElementById('jackpotCardsContainer');
  var drawSortTabsSectionEl = document.getElementById('drawSortTabsSection');
  var sortClosestDrawBtn = document.getElementById('sortClosestDrawBtn');
  var sortBiggestJackpotBtn = document.getElementById('sortBiggestJackpotBtn');
  var sortCheaperTicketsBtn = document.getElementById('sortCheaperTicketsBtn');
  var currentDisplayedDrawId = null;
  var appShellEl = document.getElementById('appShell');
  var appLoadingShellEl = document.getElementById('appLoadingShell');

  var myTicketsEmptyEl = document.getElementById('myTicketsEmpty');
  var myTicketsListEl = document.getElementById('myTicketsList');
  var debugModeBadgeEl = document.getElementById('debugModeBadge');

  var ticketPurchaseScreenEl = document.getElementById('ticketPurchaseScreen');
  var ticketPurchasePanelEl = document.getElementById('ticketPurchasePanel');
  var closeTicketPurchaseScreenBtn = document.getElementById('closeTicketPurchaseScreenBtn');
  var ticketPurchaseSubtitleEl = document.getElementById('ticketPurchaseSubtitle');
  var ticketPurchaseDrawIdEl = document.getElementById('ticketPurchaseDrawId');
  var ticketPurchaseDrawCostEl = document.getElementById('ticketPurchaseDrawCost');
  var ticketPurchaseScreenStatusEl = document.getElementById('ticketPurchaseScreenStatus');
  var ticketPurchaseTicketsListEl = document.getElementById('ticketPurchaseTicketsList');
  var ticketPurchaseFooterEl = document.getElementById('ticketPurchaseFooter');
  var submitTicketPurchaseBtn = document.getElementById('submitTicketPurchaseBtn');
  var submitTicketPurchaseLabelEl = document.getElementById('submitTicketPurchaseLabel');
  var submitTicketPurchaseCostEl = document.getElementById('submitTicketPurchaseCost');
  var centerPopupEl = document.getElementById('centerPopup');
  var centerPopupBackdropEl = document.getElementById('centerPopupBackdrop');
  var centerPopupMessageEl = document.getElementById('centerPopupMessage');
  var centerPopupConfirmBtn = document.getElementById('centerPopupConfirmBtn');

  var highlightTicketId = null;
  var lastStateSig = null;
  var latestState = { balance: 0, serverNowUtc: null, currentDraw: null, activeDraws: [], activeTicketGroups: [], currentTickets: [], history: [], ticketPurchase: null };
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
  var currentDrawSortMode = 'closest';
  var historyEntries = [];
  var newsBanners = [];
  var winnerEntries = [];
  var newsBannerIndex = 0;
  var newsBannerAutoplayTimerId = null;
  var newsBannerGestureBound = false;
  var newsBannerDragPointerId = null;
  var newsBannerDragStartX = 0;
  var newsBannerDragDeltaX = 0;
  var newsBannerDragActive = false;
  var newsBannerDragMoved = false;
  var newsBannerSuppressClickUntil = 0;
  var drawCardListGestureBound = false;
  var drawCardListDragPointerId = null;
  var drawCardListDragStartX = 0;
  var drawCardListDragStartScrollLeft = 0;
  var drawCardListDragActive = false;
  var drawCardListDragMoved = false;
  var drawCardListSuppressClickUntil = 0;
  var referralInviteCode = '';
  var referralInviteLink = '';
  var referralCodeFromQuery = '';
  var referralIsBound = false;
  var paymentSystemsOptions = null;
  var selectedPaymentMethod = null;
  var activeDeposit = null;
  var tonConnectUi = null;
  var tonConnectWallet = null;
  var tonConnectInitPromise = null;
  var tonConnectStatusUnsubscribe = null;

  var LOTTO_NUMBERS_COUNT = 5;
  var LOTTO_MIN = 1;
  var LOTTO_MAX = 36;
  var ticketSlotsPerPurchaseScreen = 10;
  var purchaseScreenDrawId = null;
  var purchaseScreenTicketStates = [];
  var activePurchaseScreenTicketIndex = 0;
  var purchaseScreenSubmitting = false;
  var pollingIntervalId = null;
  var countdownIntervalId = null;
  var serverClockOffsetMs = 0;
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
    renderNewsBanners(newsBanners);
    renderWinners(winnerEntries);
    renderPaymentSystems();
    renderTonConnectPanel();
    updateDepositDetails(activeDeposit);
    syncDrawSortTabs();
    renderCurrentDraw(selected.draw, selected.tickets, selected.hasMultipleActiveDraws);
    renderActiveDrawBanners(getActiveDraws(latestState), getActiveTicketGroups(latestState));
    renderMyTickets(latestState);
    renderHistory();
    if (ticketPurchaseScreenEl && !ticketPurchaseScreenEl.hidden) {
      renderTicketPurchaseScreen();
    }

    if (clientIsLocalDebug && debugModeBadgeReason) {
      setDebugModeBadge(getDebugModeBadgeText(debugModeBadgeReason));
    }
  }

  function getActiveDraws(state) {
    var draws = (state && Array.isArray(state.activeDraws))
      ? state.activeDraws.filter(function (x) { return !!x && isDrawPurchasable(x); })
      : [];
    return draws.slice().sort(compareActiveDraws);
  }

  function getActiveTicketGroups(state) {
    return (state && Array.isArray(state.activeTicketGroups)) ? state.activeTicketGroups : [];
  }

  function getTicketPurchaseConfig(state) {
    return state && state.ticketPurchase ? state.ticketPurchase : null;
  }

  function getDrawSortCloseMs(draw) {
    var closeMs = getDrawPurchaseCloseMs(draw);
    if (closeMs != null) return closeMs;

    var createdMs = draw && draw.createdAtUtc ? Date.parse(draw.createdAtUtc) : NaN;
    return Number.isFinite(createdMs) ? createdMs : Number.MAX_SAFE_INTEGER;
  }

  function compareNumbersAsc(a, b) {
    return (Number(a) || 0) - (Number(b) || 0);
  }

  function compareNumbersDesc(a, b) {
    return (Number(b) || 0) - (Number(a) || 0);
  }

  function compareActiveDraws(a, b) {
    if (!a && !b) return 0;
    if (!a) return 1;
    if (!b) return -1;

    if (currentDrawSortMode === 'jackpot') {
      return compareNumbersDesc(a.prizePoolMatch5, b.prizePoolMatch5)
        || compareNumbersAsc(getDrawSortCloseMs(a), getDrawSortCloseMs(b))
        || compareNumbersAsc(a.ticketCost, b.ticketCost)
        || compareNumbersDesc(a.id, b.id);
    }

    if (currentDrawSortMode === 'cheap') {
      return compareNumbersAsc(a.ticketCost, b.ticketCost)
        || compareNumbersDesc(a.prizePoolMatch5, b.prizePoolMatch5)
        || compareNumbersAsc(getDrawSortCloseMs(a), getDrawSortCloseMs(b))
        || compareNumbersDesc(a.id, b.id);
    }

    return compareNumbersAsc(getDrawSortCloseMs(a), getDrawSortCloseMs(b))
      || compareNumbersDesc(a.prizePoolMatch5, b.prizePoolMatch5)
      || compareNumbersAsc(a.ticketCost, b.ticketCost)
      || compareNumbersDesc(a.id, b.id);
  }

  function syncDrawSortTabs() {
    var mapping = [
      { button: sortClosestDrawBtn, mode: 'closest' },
      { button: sortBiggestJackpotBtn, mode: 'jackpot' },
      { button: sortCheaperTicketsBtn, mode: 'cheap' }
    ];

    var activeButton = null;

    mapping.forEach(function (item) {
      if (!item.button) return;
      var isActive = currentDrawSortMode === item.mode;
      item.button.classList.toggle('draw-sort-tab-active', isActive);
      item.button.setAttribute('aria-selected', isActive ? 'true' : 'false');
      item.button.tabIndex = isActive ? 0 : -1;
      if (isActive) {
        activeButton = item.button;
      }
    });

    if (activeButton && typeof activeButton.scrollIntoView === 'function') {
      try {
        activeButton.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'nearest' });
      } catch (e) {
        try {
          activeButton.scrollIntoView(false);
        } catch (e) {
        }
      }
    }
  }

  function setDrawSortMode(mode) {
    var normalized = String(mode || '').toLowerCase();
    if (normalized !== 'closest' && normalized !== 'jackpot' && normalized !== 'cheap') {
      normalized = 'closest';
    }

    if (currentDrawSortMode === normalized) {
      syncDrawSortTabs();
      return;
    }

    currentDrawSortMode = normalized;
    syncDrawSortTabs();

    var selected = resolveSelectedDrawSnapshot(latestState);
    renderCurrentDraw(selected.draw, selected.tickets, selected.hasMultipleActiveDraws);
    renderActiveDrawBanners(getActiveDraws(latestState), getActiveTicketGroups(latestState));
  }

  function getDrawCardThemeClass(cardColor) {
    var value = String(cardColor || '').trim().toLowerCase();
    if (value === 'teal' || value === 'pink' || value === 'blue' || value === 'orange') {
      return 'draw-card-theme-' + value;
    }

    return 'draw-card-theme-gold';
  }

  function applyDrawCardTheme(cardEl, draw) {
    if (!cardEl) return;

    cardEl.classList.remove(
      'draw-card-theme-gold',
      'draw-card-theme-teal',
      'draw-card-theme-pink',
      'draw-card-theme-blue',
      'draw-card-theme-orange');

    cardEl.classList.add(getDrawCardThemeClass(draw && draw.cardColor));
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
    if (topUpTonStatusEl) topUpTonStatusEl.textContent = text || '';
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

  function setWinnersStatus(text) {
    if (winnersStatusEl) winnersStatusEl.textContent = text || '';
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

  function syncServerClock(serverNowUtc) {
    var serverNowMs = serverNowUtc ? Date.parse(serverNowUtc) : NaN;
    if (!Number.isFinite(serverNowMs)) return;

    serverClockOffsetMs = serverNowMs - Date.now();
  }

  function getServerNowMs() {
    return Date.now() + serverClockOffsetMs;
  }

  function getDrawPurchaseCloseMs(draw) {
    var closeMs = draw && draw.purchaseClosesAtUtc ? Date.parse(draw.purchaseClosesAtUtc) : NaN;
    return Number.isFinite(closeMs) ? closeMs : null;
  }

  function formatCountdown(totalSeconds) {
    var safeSeconds = Math.max(0, Math.floor(Number(totalSeconds || 0)));
    var hours = Math.floor(safeSeconds / 3600);
    var minutes = Math.floor((safeSeconds % 3600) / 60);
    var seconds = safeSeconds % 60;

    if (hours > 0) {
      return String(hours).padStart(2, '0')
        + ':' + String(minutes).padStart(2, '0')
        + ':' + String(seconds).padStart(2, '0');
    }

    return String(minutes).padStart(2, '0')
      + ':' + String(seconds).padStart(2, '0');
  }

  function isDrawPurchasable(draw) {
    if (!draw || draw.state !== 'active' || draw.canPurchase === false) {
      return false;
    }

    var closeMs = getDrawPurchaseCloseMs(draw);
    return closeMs != null && closeMs > getServerNowMs();
  }

  function getDrawCountdownText(draw) {
    if (!draw) {
      return t('client.jackpot.endsIn', 'Ends in --:--:--');
    }

    if (draw.state !== 'active') {
      return formatDrawStateLabel(draw.state);
    }

    var closeMs = getDrawPurchaseCloseMs(draw);
    if (closeMs == null) {
      return t('client.drawCard.noSchedule', 'Schedule pending');
    }

    var remainingSeconds = Math.floor((closeMs - getServerNowMs()) / 1000);
    if (draw.canPurchase === false || remainingSeconds <= 0) {
      return t('client.drawCard.salesClosed', 'Sales closed');
    }

    return formatCountdown(remainingSeconds);
  }

  function getDrawStatusText(draw) {
    if (!draw) return '';
    if (draw.state !== 'active') return formatDrawStateLabel(draw.state);
    if (!isDrawPurchasable(draw)) return t('client.drawCard.salesClosed', 'Sales closed');
    return '';
  }

  function getDrawUnavailableMessage(draw) {
    if (draw && draw.state === 'active' && !isDrawPurchasable(draw)) {
      return t('client.status.drawClosed', 'Ticket sales for this draw are closed.');
    }

    return t('client.status.noActiveDraw', 'There is no active draw right now.');
  }

  function refreshCountdownVisuals() {
    var selected = resolveSelectedDrawSnapshot(latestState);
    renderCurrentDraw(selected.draw, selected.tickets, selected.hasMultipleActiveDraws);
    renderActiveDrawBanners(getActiveDraws(latestState), getActiveTicketGroups(latestState));
    if (ticketPurchaseScreenEl && !ticketPurchaseScreenEl.hidden) {
      renderTicketPurchaseScreen();
    }
  }

  function startCountdownTimer() {
    if (countdownIntervalId) return;

    try {
      countdownIntervalId = setInterval(function () {
        refreshCountdownVisuals();
      }, 1000);
    } catch (e) {
      countdownIntervalId = null;
    }
  }

  function renderBalance(balanceValue) {
    var formatted = formatCurrency(balanceValue);
    if (userBalanceTextEl) userBalanceTextEl.textContent = formatted;
    if (profileBalanceTextEl) profileBalanceTextEl.textContent = formatted;
  }

  function stopNewsBannerAutoplay() {
    if (!newsBannerAutoplayTimerId) return;

    try {
      clearInterval(newsBannerAutoplayTimerId);
    } catch (e) {
    }

    newsBannerAutoplayTimerId = null;
  }

  function updateNewsBannerPosition(offsetPx) {
    if (newsBannerTrackEl) {
      var pixelOffset = Number(offsetPx || 0);
      if (pixelOffset === 0) {
        newsBannerTrackEl.style.transform = 'translateX(-' + (newsBannerIndex * 100) + '%)';
      } else {
        newsBannerTrackEl.style.transform = 'translateX(calc(-' + (newsBannerIndex * 100) + '% + ' + pixelOffset + 'px))';
      }
    }

    if (!newsBannerDotsEl) return;

    var dots = newsBannerDotsEl.querySelectorAll('.news-banner-dot');
    for (var i = 0; i < dots.length; i++) {
      var isActive = i === newsBannerIndex;
      dots[i].classList.toggle('news-banner-dot-active', isActive);
      dots[i].setAttribute('aria-current', isActive ? 'true' : 'false');
    }
  }

  function getNewsBannerSwipeThreshold() {
    var width = newsBannerCarouselEl ? Number(newsBannerCarouselEl.clientWidth || 0) : 0;
    return Math.max(30, Math.round(width * 0.12));
  }

  function getNewsBannerAdjustedDragOffset(deltaX) {
    var value = Number(deltaX || 0);
    if (newsBanners.length <= 1) return 0;

    if ((newsBannerIndex <= 0 && value > 0) || (newsBannerIndex >= newsBanners.length - 1 && value < 0)) {
      value *= 0.35;
    }

    return value;
  }

  function endNewsBannerDrag(pointerId) {
    if (!newsBannerDragActive || (pointerId != null && newsBannerDragPointerId !== pointerId)) return;

    var swipeThreshold = getNewsBannerSwipeThreshold();
    var finalDelta = newsBannerDragDeltaX;
    var shouldAdvance = Math.abs(finalDelta) >= swipeThreshold;
    var shouldSuppressClick = newsBannerDragMoved;

    if (shouldAdvance && newsBanners.length > 1) {
      if (finalDelta < 0 && newsBannerIndex < newsBanners.length - 1) {
        newsBannerIndex++;
      } else if (finalDelta > 0 && newsBannerIndex > 0) {
        newsBannerIndex--;
      }
    }

    newsBannerDragPointerId = null;
    newsBannerDragActive = false;
    newsBannerDragStartX = 0;
    newsBannerDragDeltaX = 0;
    newsBannerDragMoved = false;

    if (newsBannerCarouselEl) {
      newsBannerCarouselEl.classList.remove('news-banner-carousel-dragging');
    }

    if (shouldSuppressClick) {
      newsBannerSuppressClickUntil = Date.now() + 250;
    }

    updateNewsBannerPosition();
    startNewsBannerAutoplay();
  }

  function endDrawCardListDrag(pointerId) {
    if (!drawCardListDragActive || (pointerId != null && drawCardListDragPointerId !== pointerId)) return;

    var shouldSuppressClick = drawCardListDragMoved;

    drawCardListDragPointerId = null;
    drawCardListDragStartX = 0;
    drawCardListDragStartScrollLeft = 0;
    drawCardListDragActive = false;
    drawCardListDragMoved = false;

    if (jackpotCardsContainerEl) {
      jackpotCardsContainerEl.classList.remove('draw-card-list-dragging');
    }

    if (shouldSuppressClick) {
      drawCardListSuppressClickUntil = Date.now() + 250;
    }
  }

  function bindNewsBannerGestures() {
    if (!newsBannerCarouselEl || newsBannerGestureBound) return;

    newsBannerGestureBound = true;

    newsBannerCarouselEl.addEventListener('pointerdown', function (event) {
      if (!event) return;
      if (event.pointerType === 'mouse' && event.button !== 0) return;
      if (newsBanners.length <= 1) return;

      if ((event.pointerType === 'mouse' || event.pointerType === 'pen') && event.cancelable) {
        event.preventDefault();
      }

      newsBannerDragPointerId = event.pointerId;
      newsBannerDragActive = true;
      newsBannerDragMoved = false;
      newsBannerDragStartX = Number(event.clientX || 0);
      newsBannerDragDeltaX = 0;
      stopNewsBannerAutoplay();

      if (newsBannerCarouselEl) {
        newsBannerCarouselEl.classList.add('news-banner-carousel-dragging');
      }

      try {
        if (typeof newsBannerCarouselEl.setPointerCapture === 'function') {
          newsBannerCarouselEl.setPointerCapture(event.pointerId);
        }
      } catch (e) {
      }
    });

    newsBannerCarouselEl.addEventListener('pointermove', function (event) {
      if (!newsBannerDragActive || !event || newsBannerDragPointerId !== event.pointerId) return;

      newsBannerDragDeltaX = Number(event.clientX || 0) - newsBannerDragStartX;
      if (Math.abs(newsBannerDragDeltaX) > 8) {
        newsBannerDragMoved = true;
      }

      if (newsBannerDragMoved && event.cancelable) {
        event.preventDefault();
      }

      updateNewsBannerPosition(getNewsBannerAdjustedDragOffset(newsBannerDragDeltaX));
    });

    newsBannerCarouselEl.addEventListener('pointerup', function (event) {
      endNewsBannerDrag(event && event.pointerId);
    });

    newsBannerCarouselEl.addEventListener('pointercancel', function (event) {
      endNewsBannerDrag(event && event.pointerId);
    });

    newsBannerCarouselEl.addEventListener('lostpointercapture', function (event) {
      endNewsBannerDrag(event && event.pointerId);
    });

    newsBannerCarouselEl.addEventListener('dragstart', function (event) {
      if (event && event.cancelable) {
        event.preventDefault();
      }
    });
  }

  function bindDrawCardListGestures() {
    if (!jackpotCardsContainerEl || drawCardListGestureBound) return;

    drawCardListGestureBound = true;

    jackpotCardsContainerEl.addEventListener('pointerdown', function (event) {
      if (!event) return;
      if (event.pointerType === 'touch') return;
      if (event.pointerType === 'mouse' && event.button !== 0) return;
      if (jackpotCardsContainerEl.scrollWidth <= jackpotCardsContainerEl.clientWidth) return;

      drawCardListDragPointerId = event.pointerId;
      drawCardListDragStartX = Number(event.clientX || 0);
      drawCardListDragStartScrollLeft = Number(jackpotCardsContainerEl.scrollLeft || 0);
      drawCardListDragActive = true;
      drawCardListDragMoved = false;
      jackpotCardsContainerEl.classList.add('draw-card-list-dragging');

      if (event.cancelable) {
        event.preventDefault();
      }

      try {
        if (typeof jackpotCardsContainerEl.setPointerCapture === 'function') {
          jackpotCardsContainerEl.setPointerCapture(event.pointerId);
        }
      } catch (e) {
      }
    });

    jackpotCardsContainerEl.addEventListener('pointermove', function (event) {
      if (!drawCardListDragActive || !event || drawCardListDragPointerId !== event.pointerId) return;

      var deltaX = Number(event.clientX || 0) - drawCardListDragStartX;
      if (Math.abs(deltaX) > 6) {
        drawCardListDragMoved = true;
      }

      if (drawCardListDragMoved && event.cancelable) {
        event.preventDefault();
      }

      jackpotCardsContainerEl.scrollLeft = drawCardListDragStartScrollLeft - deltaX;
    });

    jackpotCardsContainerEl.addEventListener('pointerup', function (event) {
      endDrawCardListDrag(event && event.pointerId);
    });

    jackpotCardsContainerEl.addEventListener('pointercancel', function (event) {
      endDrawCardListDrag(event && event.pointerId);
    });

    jackpotCardsContainerEl.addEventListener('lostpointercapture', function (event) {
      endDrawCardListDrag(event && event.pointerId);
    });

    jackpotCardsContainerEl.addEventListener('dragstart', function (event) {
      if (event && event.cancelable) {
        event.preventDefault();
      }
    });

    jackpotCardsContainerEl.addEventListener('click', function (event) {
      if (Date.now() >= drawCardListSuppressClickUntil) return;
      if (event) {
        event.preventDefault();
        event.stopPropagation();
      }
    }, true);
  }

  function startNewsBannerAutoplay() {
    stopNewsBannerAutoplay();

    if (!newsBannerSectionEl || newsBannerSectionEl.hidden || newsBanners.length <= 1) {
      return;
    }

    newsBannerAutoplayTimerId = setInterval(function () {
      if (document.hidden || newsBanners.length <= 1) return;

      newsBannerIndex = (newsBannerIndex + 1) % newsBanners.length;
      updateNewsBannerPosition();
    }, 4500);
  }

  function normalizeNewsBannerActionType(actionType) {
    var value = String(actionType || '').trim().toLowerCase();
    if (value === 'app_section' || value === 'external_url') return value;
    return 'none';
  }

  function getNewsBannerActionValue(banner) {
    if (!banner) return '';
    return String(banner.actionValue || '').trim();
  }

  function isNewsBannerInteractive(banner) {
    var actionType = normalizeNewsBannerActionType(banner && banner.actionType);
    return actionType !== 'none' && getNewsBannerActionValue(banner).length > 0;
  }

  function openNewsBannerAction(banner) {
    var actionType = normalizeNewsBannerActionType(banner && banner.actionType);
    var actionValue = getNewsBannerActionValue(banner);
    if (actionType === 'none' || !actionValue) return;

    if (actionType === 'external_url') {
      openCheckoutLink(actionValue);
      return;
    }

    if (actionValue === 'lottery') {
      setActiveTab('lottery');
      return;
    }

    if (actionValue === 'tickets') {
      setActiveTab('tickets');
      return;
    }

    if (actionValue === 'winners') {
      setActiveTab('winners');
      return;
    }

    if (actionValue === 'profile') {
      setActiveTab('profile');
      return;
    }

    if (actionValue.indexOf('profile/') === 0) {
      setActiveTab('profile');
      setProfileScreen(actionValue.substring('profile/'.length));
    }
  }

  function renderNewsBanners(items) {
    newsBanners = Array.isArray(items)
      ? items.filter(function (item) {
          return item && item.imageUrl && String(item.imageUrl).trim().length > 0;
        })
      : [];

    if (!newsBannerSectionEl) newsBannerSectionEl = document.getElementById('newsBannerSection');
    if (!newsBannerCarouselEl) newsBannerCarouselEl = document.getElementById('newsBannerCarousel');
    if (!newsBannerTrackEl) newsBannerTrackEl = document.getElementById('newsBannerTrack');
    if (!newsBannerDotsEl) newsBannerDotsEl = document.getElementById('newsBannerDots');

    if (!newsBannerSectionEl || !newsBannerCarouselEl || !newsBannerTrackEl || !newsBannerDotsEl) {
      return;
    }

    bindNewsBannerGestures();

    stopNewsBannerAutoplay();
    newsBannerTrackEl.innerHTML = '';
    newsBannerDotsEl.innerHTML = '';

    if (newsBanners.length === 0) {
      newsBannerSectionEl.hidden = true;
      newsBannerIndex = 0;
      return;
    }

    newsBannerIndex = Math.min(newsBannerIndex, newsBanners.length - 1);
    if (newsBannerIndex < 0) newsBannerIndex = 0;
    newsBannerSectionEl.hidden = false;

    newsBanners.forEach(function (banner, index) {
      var actionType = normalizeNewsBannerActionType(banner && banner.actionType);
      var interactive = isNewsBannerInteractive(banner);
      var slide = document.createElement(interactive ? 'button' : 'article');
      slide.className = 'news-banner-slide';
      slide.draggable = false;

      if (interactive) {
        slide.type = 'button';
        slide.classList.add('news-banner-slide-actionable');
        slide.setAttribute('aria-label', t('client.news.bannerAltPrefix', 'News banner') + ' ' + (index + 1));
        slide.addEventListener('click', function (event) {
          if (Date.now() < newsBannerSuppressClickUntil) {
            if (event) {
              event.preventDefault();
              event.stopPropagation();
            }
            return;
          }

          openNewsBannerAction(banner);
        });
      }

      var image = document.createElement('img');
      image.className = 'news-banner-image';
      image.src = String(banner.imageUrl || '');
      image.alt = t('client.news.bannerAltPrefix', 'News banner') + ' ' + (index + 1);
      image.decoding = 'async';
      image.loading = index === 0 ? 'eager' : 'lazy';
      image.draggable = false;

      if (interactive && actionType === 'external_url') {
        image.alt = image.alt + ' ↗';
      }

      slide.appendChild(image);
      newsBannerTrackEl.appendChild(slide);

      var dot = document.createElement('button');
      dot.type = 'button';
      dot.className = 'news-banner-dot';
      dot.setAttribute('aria-label', t('client.news.dotAriaPrefix', 'Go to banner') + ' ' + (index + 1));
      dot.addEventListener('click', function () {
        newsBannerIndex = index;
        updateNewsBannerPosition();
        startNewsBannerAutoplay();
      });

      newsBannerDotsEl.appendChild(dot);
    });

    newsBannerDotsEl.hidden = newsBanners.length <= 1;
    updateNewsBannerPosition();
    startNewsBannerAutoplay();
  }

  function getJson(url) {
    return fetch(url, {
      method: 'GET',
      headers: {
        'Accept': 'application/json'
      }
    }).then(function (r) {
      if (!r.ok) {
        throw new Error('HTTP ' + r.status);
      }

      return r.json();
    });
  }

  function loadNewsBanners() {
    return getJson('/api/news-banners')
      .then(function (res) {
        renderNewsBanners(res && Array.isArray(res.banners) ? res.banners : []);
      })
      .catch(function () {
        renderNewsBanners([]);
      });
  }

  function createWinnerEl(entry) {
    var card = document.createElement('article');
    card.className = 'winner-card';

    var photo = document.createElement('img');
    photo.className = 'winner-photo';
    photo.src = String(entry.photoUrl || '');
    photo.alt = String(entry.name || '').trim() || t('client.winners.title', 'Winners');
    photo.decoding = 'async';
    photo.loading = 'lazy';

    var body = document.createElement('div');
    body.className = 'winner-body';

    var name = document.createElement('div');
    name.className = 'winner-name';
    name.textContent = String(entry.name || '').trim();

    var amount = document.createElement('div');
    amount.className = 'winner-amount';
    amount.textContent = String(entry.winningAmount || '').trim();

    body.appendChild(name);
    body.appendChild(amount);
    var quote = String(entry.quote || '').trim();
    if (quote) {
      var quoteEl = document.createElement('div');
      quoteEl.className = 'winner-quote';
      quoteEl.textContent = '«' + quote + '»';
      body.appendChild(quoteEl);
    }
    card.appendChild(photo);
    card.appendChild(body);

    return card;
  }

  function renderWinners(items) {
    winnerEntries = Array.isArray(items)
      ? items.filter(function (item) {
          return item && item.photoUrl && item.name && item.winningAmount;
        })
      : [];

    if (!winnersListEl) winnersListEl = document.getElementById('winnersList');
    if (!winnersEmptyEl) winnersEmptyEl = document.getElementById('winnersEmpty');
    if (!winnersListEl || !winnersEmptyEl) return;

    winnersListEl.innerHTML = '';
    winnersEmptyEl.hidden = winnerEntries.length > 0;

    winnerEntries.forEach(function (entry) {
      winnersListEl.appendChild(createWinnerEl(entry));
    });
  }

  function loadWinners() {
    setWinnersStatus(t('client.winners.loading', 'Loading winners...'));

    return getJson('/api/winners')
      .then(function (res) {
        renderWinners(res && Array.isArray(res.winners) ? res.winners : []);
        setWinnersStatus('');
      })
      .catch(function () {
        renderWinners([]);
        setWinnersStatus(t('client.winners.loadFailed', 'Failed to load winners.'));
      });
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

  function setTicketPurchaseScreenStatus(text) {
    if (ticketPurchaseScreenStatusEl) ticketPurchaseScreenStatusEl.textContent = text || '';
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
      || value.indexOf('selected tickets was already purchased') >= 0
      || value.indexOf('different number combinations') >= 0
      || value.indexOf('numbers must be unique') >= 0
      || value.indexOf('choose 5 unique numbers') >= 0
      || value.indexOf('complete at least one ticket') >= 0;
  }

  function createEmptyPurchaseTicketState(slotNumber) {
    return {
      slotNumber: slotNumber,
      numbers: []
    };
  }

  function normalizeTicketNumbers(numbers) {
    return (numbers || [])
      .map(function (value) { return parseInt(value, 10); })
      .filter(function (value) {
        return Number.isFinite(value) && value >= LOTTO_MIN && value <= LOTTO_MAX;
      })
      .filter(function (value, index, arr) { return arr.indexOf(value) === index; })
      .sort(function (a, b) { return a - b; })
      .slice(0, LOTTO_NUMBERS_COUNT);
  }

  function ensurePurchaseScreenTicketStates() {
    var targetCount = Math.max(1, parseInt(ticketSlotsPerPurchaseScreen, 10) || 10);
    var next = [];

    for (var i = 0; i < targetCount; i++) {
      var existing = purchaseScreenTicketStates[i];
      next.push({
        slotNumber: i + 1,
        numbers: normalizeTicketNumbers(existing && existing.numbers)
      });
    }

    purchaseScreenTicketStates = next;

    if (!Number.isFinite(activePurchaseScreenTicketIndex)) {
      activePurchaseScreenTicketIndex = 0;
    }

    if (purchaseScreenTicketStates.length === 0) {
      activePurchaseScreenTicketIndex = 0;
      return;
    }

    if (activePurchaseScreenTicketIndex < 0) {
      activePurchaseScreenTicketIndex = 0;
    } else if (activePurchaseScreenTicketIndex >= purchaseScreenTicketStates.length) {
      activePurchaseScreenTicketIndex = purchaseScreenTicketStates.length - 1;
    }
  }

  function setActivePurchaseScreenTicket(index, options) {
    ensurePurchaseScreenTicketStates();
    if (purchaseScreenTicketStates.length === 0) return;

    var normalizedIndex = Math.max(0, Math.min(purchaseScreenTicketStates.length - 1, parseInt(index, 10) || 0));
    var changed = activePurchaseScreenTicketIndex !== normalizedIndex;
    activePurchaseScreenTicketIndex = normalizedIndex;

    if (!changed && !(options && options.forceRender)) {
      return;
    }

    renderTicketPurchaseScreen();

    if (options && options.scrollIntoView && ticketPurchaseTicketsListEl) {
      var activeCard = ticketPurchaseTicketsListEl.querySelector('[data-purchase-ticket-index="' + normalizedIndex + '"]');
      if (activeCard && typeof activeCard.scrollIntoView === 'function') {
        try {
          activeCard.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        } catch (e) {
          activeCard.scrollIntoView();
        }
      }
    }
  }

  function getPurchaseScreenDraw() {
    var activeDraws = getActiveDraws(latestState);
    if (purchaseScreenDrawId != null) {
      for (var i = 0; i < activeDraws.length; i++) {
        if (activeDraws[i] && activeDraws[i].id === purchaseScreenDrawId) {
          return activeDraws[i];
        }
      }
    }

    var snapshot = resolveSelectedDrawSnapshot(latestState);
    return snapshot.draw && snapshot.draw.state === 'active' ? snapshot.draw : null;
  }

  function isPurchaseTicketComplete(ticketState) {
    return !!ticketState && Array.isArray(ticketState.numbers) && ticketState.numbers.length === LOTTO_NUMBERS_COUNT;
  }

  function getCompletedPurchaseTickets() {
    return purchaseScreenTicketStates.filter(isPurchaseTicketComplete);
  }

  function getPurchaseTicketSignature(ticketState) {
    return normalizeTicketNumbers(ticketState && ticketState.numbers).join(',');
  }

  function getDuplicateCompletedTicketSignature() {
    var signatures = {};
    var completed = getCompletedPurchaseTickets();

    for (var i = 0; i < completed.length; i++) {
      var signature = getPurchaseTicketSignature(completed[i]);
      if (!signature) continue;
      if (signatures[signature]) return signature;
      signatures[signature] = true;
    }

    return null;
  }

  function getPurchaseScreenValidation() {
    var completed = getCompletedPurchaseTickets();
    if (completed.length === 0) {
      return {
        ok: false,
        message: t('client.purchaseScreen.selectAtLeastOne', 'Complete at least one ticket to continue.'),
        completedCount: 0
      };
    }

    if (getDuplicateCompletedTicketSignature()) {
      return {
        ok: false,
        message: t('client.purchaseScreen.duplicateTickets', 'Completed tickets must use different number combinations.'),
        completedCount: completed.length
      };
    }

    return { ok: true, message: '', completedCount: completed.length };
  }

  function getRandomTicketNumbers() {
    var set = new Set();
    while (set.size < LOTTO_NUMBERS_COUNT) {
      set.add(LOTTO_MIN + Math.floor(Math.random() * (LOTTO_MAX - LOTTO_MIN + 1)));
    }

    return Array.from(set).sort(function (a, b) { return a - b; });
  }

  function setPurchaseScreenTicketNumbers(index, numbers) {
    if (index < 0 || index >= purchaseScreenTicketStates.length) return;
    activePurchaseScreenTicketIndex = index;
    purchaseScreenTicketStates[index].numbers = normalizeTicketNumbers(numbers);
    renderTicketPurchaseScreen();
  }

  function clearPurchaseScreenTicket(index) {
    setPurchaseScreenTicketNumbers(index, []);
  }

  function randomizePurchaseScreenTicket(index) {
    setPurchaseScreenTicketNumbers(index, getRandomTicketNumbers());
  }

  function togglePurchaseScreenNumber(index, number) {
    if (index < 0 || index >= purchaseScreenTicketStates.length) return;

    var ticketState = purchaseScreenTicketStates[index];
    var next = normalizeTicketNumbers(ticketState.numbers);
    var existingIndex = next.indexOf(number);

    if (existingIndex >= 0) {
      next.splice(existingIndex, 1);
    } else {
      if (next.length >= LOTTO_NUMBERS_COUNT) return;
      next.push(number);
    }

    setPurchaseScreenTicketNumbers(index, next);
  }

  function formatPurchaseScreenChooseText(missingCount) {
    return t('client.purchaseScreen.choosePrefix', 'Choose ')
      + missingCount
      + t('client.purchaseScreen.chooseSuffix', ' more number(s)');
  }

  function formatPurchaseTicketsButtonLabel(count) {
    return t('client.purchaseScreen.purchasePrefix', 'Purchase ')
      + count
      + t(count === 1
          ? 'client.purchaseScreen.purchaseSingularSuffix'
          : 'client.purchaseScreen.purchasePluralSuffix',
        count === 1 ? ' ticket' : ' tickets');
  }

  function formatPurchasedTicketsMessage(count) {
    return t('client.purchaseScreen.purchasedPrefix', 'Purchased ')
      + count
      + t('client.purchaseScreen.purchasedSuffix', ' ticket(s).');
  }

  function setSheetOpenClass() {
    var hasOpenSheet = !!(
      (ticketPurchaseScreenEl && !ticketPurchaseScreenEl.hidden)
    );

    document.body.classList.toggle('sheet-open', hasOpenSheet);
  }

  function syncTicketPurchaseFooterVisibility(showFooter) {
    var shouldShow = !!showFooter;

    if (!ticketPurchasePanelEl) {
      ticketPurchasePanelEl = document.getElementById('ticketPurchasePanel');
    }

    if (ticketPurchaseFooterEl) {
      ticketPurchaseFooterEl.hidden = !shouldShow;
    }

    if (ticketPurchasePanelEl) {
      ticketPurchasePanelEl.classList.toggle('purchase-screen-panel-footer-visible', shouldShow);
    }
  }

  function createPurchaseTicketCard(ticketState, index) {
    var card = document.createElement('section');
    card.className = 'purchase-ticket-card';
    card.setAttribute('data-purchase-ticket-index', String(index));

    var completed = isPurchaseTicketComplete(ticketState);
    if (completed) card.classList.add('purchase-ticket-card-complete');
    if (index === activePurchaseScreenTicketIndex) card.classList.add('purchase-ticket-card-active');

    card.addEventListener('click', function (event) {
      if (event && event.target && typeof event.target.closest === 'function' && event.target.closest('button')) {
        return;
      }

      setActivePurchaseScreenTicket(index, { scrollIntoView: true });
    });

    var header = document.createElement('div');
    header.className = 'purchase-ticket-card-header';

    var titleWrap = document.createElement('div');
    titleWrap.className = 'purchase-ticket-card-title-wrap';

    var title = document.createElement('div');
    title.className = 'purchase-ticket-card-title';
    title.textContent = t('client.purchaseScreen.ticketPrefix', 'Ticket') + ' ' + ticketState.slotNumber;

    var subtitle = document.createElement('div');
    subtitle.className = 'purchase-ticket-card-subtitle';
    subtitle.textContent = completed
      ? t('client.purchaseScreen.ready', 'Ready to purchase')
      : formatPurchaseScreenChooseText(Math.max(0, LOTTO_NUMBERS_COUNT - ticketState.numbers.length));

    titleWrap.appendChild(title);
    titleWrap.appendChild(subtitle);

    var actions = document.createElement('div');
    actions.className = 'purchase-ticket-card-actions';

    var randomBtn = document.createElement('button');
    randomBtn.type = 'button';
    randomBtn.className = 'purchase-ticket-card-action purchase-ticket-card-random';
    randomBtn.textContent = t('client.purchaseScreen.random', 'Random numbers');
    randomBtn.addEventListener('click', function () {
      randomizePurchaseScreenTicket(index);
    });

    var clearBtn = document.createElement('button');
    clearBtn.type = 'button';
    clearBtn.className = 'purchase-ticket-card-action purchase-ticket-card-clear';
    clearBtn.textContent = t('client.purchaseScreen.clear', 'Clear');
    clearBtn.disabled = ticketState.numbers.length === 0;
    clearBtn.addEventListener('click', function () {
      clearPurchaseScreenTicket(index);
    });

    actions.appendChild(randomBtn);
    actions.appendChild(clearBtn);

    header.appendChild(titleWrap);
    header.appendChild(actions);
    card.appendChild(header);

    var selectedLabel = document.createElement('div');
    selectedLabel.className = 'purchase-ticket-selected-label';
    selectedLabel.textContent = t('client.purchaseScreen.selectedNumbers', 'Selected numbers');
    card.appendChild(selectedLabel);

    var selectedRow = document.createElement('div');
    selectedRow.className = 'purchase-ticket-selected-row';
    if (ticketState.numbers.length === 0) {
      var empty = document.createElement('div');
      empty.className = 'purchase-ticket-selected-empty';
      empty.textContent = t('client.purchaseScreen.noNumbersSelected', 'No numbers selected yet.');
      selectedRow.appendChild(empty);
    } else {
      ticketState.numbers.forEach(function (number) {
        var chip = document.createElement('span');
        chip.className = 'purchase-ticket-selected-chip';
        chip.textContent = String(number);
        selectedRow.appendChild(chip);
      });
    }
    card.appendChild(selectedRow);

    var grid = document.createElement('div');
    grid.className = 'purchase-ticket-number-grid';
    for (var number = LOTTO_MIN; number <= LOTTO_MAX; number++) {
      var numberBtn = document.createElement('button');
      numberBtn.type = 'button';
      numberBtn.className = 'purchase-ticket-number-btn';
      if (ticketState.numbers.indexOf(number) >= 0) {
        numberBtn.classList.add('purchase-ticket-number-btn-selected');
      }
      numberBtn.textContent = String(number);
      (function (ticketIndex, value) {
        numberBtn.addEventListener('click', function () {
          togglePurchaseScreenNumber(ticketIndex, value);
        });
      })(index, number);
      grid.appendChild(numberBtn);
    }
    card.appendChild(grid);

    return card;
  }

  function renderTicketPurchaseScreen() {
    if (!ticketPurchaseScreenEl || !ticketPurchaseTicketsListEl) return;

    ensurePurchaseScreenTicketStates();

    var draw = getPurchaseScreenDraw();
    if (ticketPurchaseDrawIdEl) ticketPurchaseDrawIdEl.textContent = draw ? String(draw.id) : '-';
    if (ticketPurchaseDrawCostEl) ticketPurchaseDrawCostEl.textContent = draw ? formatCurrency(draw.ticketCost) : formatCurrency(0);
    if (ticketPurchaseSubtitleEl) {
      ticketPurchaseSubtitleEl.textContent = t('client.purchaseScreen.subtitle', 'Complete one or more tickets to buy them together.');
    }

    ticketPurchaseTicketsListEl.innerHTML = '';
    purchaseScreenTicketStates.forEach(function (ticketState, index) {
      ticketPurchaseTicketsListEl.appendChild(createPurchaseTicketCard(ticketState, index));
    });

    var validation = getPurchaseScreenValidation();
    var purchasable = isDrawPurchasable(draw);
    if (!purchaseScreenSubmitting) {
      if (!draw || !purchasable) {
        setTicketPurchaseScreenStatus(getDrawUnavailableMessage(draw));
      } else {
        setTicketPurchaseScreenStatus(validation.ok ? '' : validation.message);
      }
    }

    var completedCount = validation.completedCount || 0;
    var totalCost = draw ? Number(draw.ticketCost || 0) * completedCount : 0;
    syncTicketPurchaseFooterVisibility(completedCount > 0);
    if (submitTicketPurchaseLabelEl) submitTicketPurchaseLabelEl.textContent = formatPurchaseTicketsButtonLabel(completedCount);
    if (submitTicketPurchaseCostEl) submitTicketPurchaseCostEl.textContent = formatCurrency(totalCost);
    if (submitTicketPurchaseBtn) {
      submitTicketPurchaseBtn.disabled = purchaseScreenSubmitting || !validation.ok || !draw || !purchasable;
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

    if (drawSortTabsSectionEl) {
      drawSortTabsSectionEl.hidden = !showMultiDrawBanners;
    }
    syncDrawSortTabs();

    if (featuredJackpotCardEl) {
      featuredJackpotCardEl.hidden = showMultiDrawBanners;
    }

    if (!showMultiDrawBanners) {
      jackpotCardsContainerEl.innerHTML = '';
      jackpotCardsContainerEl.hidden = true;
      jackpotCardsContainerEl.classList.remove('draw-card-list-dragging');
      return;
    }

    jackpotCardsContainerEl.innerHTML = '';
    jackpotCardsContainerEl.hidden = false;
    jackpotCardsContainerEl.classList.add('draw-card-list');
    bindDrawCardListGestures();

    draws.forEach(function (draw) {
      var card = document.createElement('article');
      card.className = 'card draw-card draw-card-compact duplicate-jackpot-card';
      applyDrawCardTheme(card, draw);
      if (selectedActiveDrawId != null && draw.id === selectedActiveDrawId) {
        card.classList.add('draw-card-selected');
      }
      if (!isDrawPurchasable(draw)) {
        card.classList.add('draw-card-unavailable');
      }

      var header = document.createElement('div');
      header.className = 'draw-card-header';

      var title = document.createElement('div');
      title.className = 'draw-card-title';
      title.textContent = t('client.drawCard.titlePrefix', 'Draw #') + draw.id;

      var timer = document.createElement('div');
      timer.className = 'draw-card-timer';
      timer.textContent = getDrawCountdownText(draw);
      if (!isDrawPurchasable(draw)) timer.classList.add('draw-card-timer-closed');

      header.appendChild(title);
      header.appendChild(timer);

      var status = document.createElement('div');
      status.className = 'draw-card-status';
      status.textContent = getDrawStatusText(draw);
      status.hidden = !status.textContent;

      var jackpotLabel = document.createElement('div');
      jackpotLabel.className = 'draw-card-jackpot-label';
      jackpotLabel.textContent = t('client.drawCard.jackpotLabel', 'JACKPOT');

      var amount = document.createElement('div');
      amount.className = 'draw-card-jackpot-value';
      amount.textContent = formatJackpot(draw.prizePoolMatch5);

      var footer = document.createElement('div');
      footer.className = 'draw-card-footer';

      var costBlock = document.createElement('div');
      costBlock.className = 'draw-card-cost-block';

      var costLabel = document.createElement('div');
      costLabel.className = 'draw-card-cost-label';
      costLabel.textContent = t('client.drawCard.ticketCostLabel', 'Ticket cost');

      var costValue = document.createElement('div');
      costValue.className = 'draw-card-cost-value';
      costValue.textContent = formatCurrency(draw.ticketCost);

      costBlock.appendChild(costLabel);
      costBlock.appendChild(costValue);
      footer.appendChild(costBlock);

      card.appendChild(header);
      card.appendChild(status);
      card.appendChild(jackpotLabel);
      card.appendChild(amount);
      card.appendChild(footer);

      card.addEventListener('click', function () {
        if (Date.now() < drawCardListSuppressClickUntil) {
          return;
        }

        selectedActiveDrawId = draw.id;
        currentDisplayedDrawId = draw.id;
        if (!isDrawPurchasable(draw)) {
          setPurchaseStatus(getDrawUnavailableMessage(draw));
          return;
        }

        setPurchaseStatus('');
        openTicketPurchaseScreen(draw.id);
      });

      jackpotCardsContainerEl.appendChild(card);
    });
  }

  function renderCurrentDraw(draw, currentTickets) {
    // Re-bind key nodes if DOM was not ready at initial script evaluation.
    if (!currentDrawStateBadgeEl) currentDrawStateBadgeEl = document.getElementById('currentDrawStateBadge') || document.getElementById('jackpotGameStateBadge');
    if (!currentDrawPrizeTiersEl) currentDrawPrizeTiersEl = document.getElementById('currentDrawPrizeTiers');
    if (!currentDrawPrizePool3El) currentDrawPrizePool3El = document.getElementById('currentDrawPrizePool3');
    if (!currentDrawPrizePool4El) currentDrawPrizePool4El = document.getElementById('currentDrawPrizePool4');
    if (!currentDrawPrizePool5El) currentDrawPrizePool5El = document.getElementById('currentDrawPrizePool5');

    // legacy picker removed; banners will be rendered instead

    if (!draw) {
      if (currentDrawStateBadgeEl) {
        currentDrawStateBadgeEl.textContent = '';
        currentDrawStateBadgeEl.hidden = true;
      }
        if (currentDrawEmptyEl) currentDrawEmptyEl.hidden = false;
        if (currentDrawContentEl) currentDrawContentEl.hidden = true;
      if (featuredJackpotCardEl) featuredJackpotCardEl.classList.remove('draw-card-unavailable');
      if (currentDrawIdEl) currentDrawIdEl.textContent = t('client.drawCard.titlePrefix', 'Draw #') + '—';
      if (currentDrawPrizePoolEl) currentDrawPrizePoolEl.textContent = '$0.00';
      if (currentDrawCreatedAtEl) currentDrawCreatedAtEl.textContent = t('client.jackpot.endsIn', 'Ends in --:--:--');
      if (jackpotAmountEl) jackpotAmountEl.textContent = '$0';
      if (jackpotSubtitleEl) jackpotSubtitleEl.textContent = t('client.drawCard.jackpotLabel', 'JACKPOT');
      if (currentDrawPrizeTiersEl) currentDrawPrizeTiersEl.setAttribute('hidden', 'hidden');
      if (currentDrawPrizePool3El) currentDrawPrizePool3El.textContent = '$0.00';
      if (currentDrawPrizePool4El) currentDrawPrizePool4El.textContent = '$0.00';
      if (currentDrawPrizePool5El) currentDrawPrizePool5El.textContent = '$0.00';
      if (currentDrawSubtitleEl) currentDrawSubtitleEl.hidden = false;
      if (currentDrawTicketPriceRowEl) currentDrawTicketPriceRowEl.hidden = false;
      if (currentDrawTicketCostEl) currentDrawTicketCostEl.textContent = '$2.00';
      if (currentDrawPurchaseBlockEl) currentDrawPurchaseBlockEl.hidden = false;
      applyDrawCardTheme(featuredJackpotCardEl, null);
      // legacy picker cleanup not required
      return;
    }

    var purchasable = isDrawPurchasable(draw);
    var isFinishedDraw = draw.state === 'finished';

    if (currentDrawStateBadgeEl) {
      currentDrawStateBadgeEl.textContent = getDrawStatusText(draw);
      currentDrawStateBadgeEl.hidden = !currentDrawStateBadgeEl.textContent;
    }
    if (jackpotChipEl) jackpotChipEl.textContent = formatDrawStateLabel(draw.state);
    if (currentDrawEmptyEl) currentDrawEmptyEl.hidden = true;
    if (currentDrawContentEl) currentDrawContentEl.hidden = false;
    if (featuredJackpotCardEl) {
      applyDrawCardTheme(featuredJackpotCardEl, draw);
      featuredJackpotCardEl.classList.toggle('draw-card-unavailable', !purchasable);
      featuredJackpotCardEl.classList.toggle('draw-card-selected', !!purchasable);
    }

    if (currentDrawIdEl) currentDrawIdEl.textContent = t('client.drawCard.titlePrefix', 'Draw #') + draw.id;
    if (currentDrawPrizePoolEl) currentDrawPrizePoolEl.textContent = '$' + formatPrizePool(draw.prizePool);
    if (currentDrawCreatedAtEl) {
      currentDrawCreatedAtEl.textContent = getDrawCountdownText(draw);
      currentDrawCreatedAtEl.classList.toggle('draw-card-timer-closed', !purchasable);
    }
    if (jackpotAmountEl) jackpotAmountEl.textContent = formatJackpot(draw.prizePoolMatch5 || 0);
    if (jackpotSubtitleEl) jackpotSubtitleEl.textContent = t('client.drawCard.jackpotLabel', 'JACKPOT');
    if (currentDrawPrizeTiersEl) currentDrawPrizeTiersEl.setAttribute('hidden', 'hidden');
    if (currentDrawPrizePool3El) currentDrawPrizePool3El.textContent = '$' + formatPrizeTierPool(draw.prizePoolMatch3 || 0);
    if (currentDrawPrizePool4El) currentDrawPrizePool4El.textContent = '$' + formatPrizeTierPool(draw.prizePoolMatch4 || 0);
    if (currentDrawPrizePool5El) currentDrawPrizePool5El.textContent = '$' + formatPrizeTierPool(draw.prizePoolMatch5 || 0);
    if (currentDrawTicketCostEl) currentDrawTicketCostEl.textContent = formatCurrency(draw.ticketCost);

    // Update jackpot purchase price and track the displayed draw id
    try {
      currentDisplayedDrawId = draw ? draw.id : null;
      var jackpotPriceEl = document.getElementById('jackpotBuyPrice');
      if (jackpotPriceEl) jackpotPriceEl.textContent = formatCurrency(draw ? draw.ticketCost : 0);
    } catch (e) {
      try { console.warn('renderCurrentDraw jackpot update error', e); } catch (e) { }
    }

    var hasNumbers = !!(draw.numbers && String(draw.numbers).length > 0);
    if (currentDrawNumbersWrapEl) currentDrawNumbersWrapEl.hidden = !hasNumbers;
    if (currentDrawNumbersEl) {
      currentDrawNumbersEl.innerHTML = '';
      if (hasNumbers) currentDrawNumbersEl.appendChild(createNumbersRow(draw.numbers, draw.numbers));
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

  function getPaymentSystems() {
    if (!paymentSystemsOptions || !Array.isArray(paymentSystemsOptions.systems)) return [];
    return paymentSystemsOptions.systems.filter(function (x) { return !!x && !!x.key; });
  }

  function getPaymentSystemByKey(key) {
    var normalized = String(key || '').trim().toLowerCase();
    var systems = getPaymentSystems();
    for (var i = 0; i < systems.length; i++) {
      if (String(systems[i].key || '').trim().toLowerCase() === normalized) return systems[i];
    }
    return null;
  }

  function getSelectedPaymentMethod() {
    var systems = getPaymentSystems();
    if (systems.length === 0) return null;
    if (selectedPaymentMethod && getPaymentSystemByKey(selectedPaymentMethod)) return selectedPaymentMethod;
    if (paymentSystemsOptions && paymentSystemsOptions.defaultPaymentMethod && getPaymentSystemByKey(paymentSystemsOptions.defaultPaymentMethod)) {
      selectedPaymentMethod = paymentSystemsOptions.defaultPaymentMethod;
      return selectedPaymentMethod;
    }
    selectedPaymentMethod = systems[0].key;
    return selectedPaymentMethod;
  }

  function setSelectedPaymentMethod(methodKey) {
    selectedPaymentMethod = getPaymentSystemByKey(methodKey) ? methodKey : getSelectedPaymentMethod();
    renderPaymentSystems();
    syncTopUpButtonLabel();
  }

  function getPaymentMethodTitle(methodKey) {
    var normalized = String(methodKey || '').trim().toLowerCase();
    if (normalized === 'telegram_ton') return t('client.topup.system.telegramTon.title', 'Telegram Wallet');
    if (normalized === 'btcpay_crypto') return t('client.topup.system.btcpay.title', 'BTCPay');
    return normalized;
  }

  function getPaymentMethodBadge(methodKey) {
    var normalized = String(methodKey || '').trim().toLowerCase();
    if (normalized === 'telegram_ton') return t('client.topup.system.telegramTon.badge', 'Fastest inside Telegram');
    if (normalized === 'btcpay_crypto') return t('client.topup.system.btcpay.badge', 'More coins');
    return '';
  }

  function getPaymentMethodDescription(methodKey) {
    var normalized = String(methodKey || '').trim().toLowerCase();
    if (normalized === 'telegram_ton') return t('client.topup.system.telegramTon.description', 'Native TON transfer inside Telegram. We generate the amount and memo automatically.');
    if (normalized === 'btcpay_crypto') return t('client.topup.system.btcpay.description', 'Create an external crypto invoice and pay with any supported wallet.');
    return '';
  }

  function createPaymentSystemIcon(methodKey) {
    var normalized = String(methodKey || '').trim().toLowerCase();
    var icon = document.createElement('span');
    icon.className = 'payment-system-icon payment-system-icon-' + normalized.replace(/[^a-z0-9]+/g, '-');

    if (normalized === 'btcpay_crypto') {
      icon.textContent = '₿';
      return icon;
    }

    if (normalized === 'telegram_ton') {
      var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
      svg.setAttribute('viewBox', '0 0 24 24');
      svg.setAttribute('aria-hidden', 'true');

      var path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
      path.setAttribute('fill', 'currentColor');
      path.setAttribute('d', 'M6.5 7.5h11c.9 0 1.46.97 1.01 1.75l-5.08 8.81a1.66 1.66 0 0 1-2.87 0L5.49 9.25A1.17 1.17 0 0 1 6.5 7.5Zm4.65 7.89V9.01H8.21c-.27 0-.43.29-.31.51l3.25 5.87Zm1.7 0 3.25-5.87c.12-.22-.04-.51-.31-.51h-2.94v6.38Z');
      svg.appendChild(path);
      icon.appendChild(svg);
      return icon;
    }

    icon.textContent = '¤';
    return icon;
  }

  function getPaymentMethodCta(methodKey) {
    var normalized = String(methodKey || '').trim().toLowerCase();
    if (normalized === 'telegram_ton') return t('client.topup.system.telegramTon.cta', 'Open Telegram Wallet');
    if (normalized === 'btcpay_crypto') return t('client.topup.system.btcpay.cta', 'Create BTCPay invoice');
    return t('client.button.deposit', 'Add funds');
  }

  function formatCryptoAssetAmount(amount, assetCode) {
    var numeric = Number(amount || 0);
    if (!Number.isFinite(numeric)) numeric = 0;
    var code = String(assetCode || '').trim().toUpperCase();
    var text = numeric.toLocaleString(getIntlLocale(), { minimumFractionDigits: numeric % 1 === 0 ? 0 : 2, maximumFractionDigits: 6 });
    return code ? text + ' ' + code : text;
  }

  function formatHistoryProvider(entry) {
    var method = String(entry && entry.paymentMethod || '').trim().toLowerCase();
    if (method === 'telegram_ton') return t('client.history.provider.telegramTon', 'Telegram Wallet · TON');
    if (method === 'btcpay_crypto') return t('client.history.provider.btcpay', 'BTCPay invoice');
    return '';
  }

  function isTelegramTonMethod(methodKey) {
    return String(methodKey || '').trim().toLowerCase() === 'telegram_ton';
  }

  function supportsTonConnect() {
    return !!(window.TON_CONNECT_UI && window.TON_CONNECT_UI.TonConnectUI && window.TonWeb && window.location && /^https:$/i.test(window.location.protocol || ''));
  }

  function getTonConnectManifestUrl() {
    var manifestVersion = '20260426b';

    try {
      var pathname = String(window.location && window.location.pathname || '/app');
      var normalizedPath = pathname.replace(/\/+$/, '') || '/';
      var lastSlashIndex = normalizedPath.lastIndexOf('/');
      var manifestPath = lastSlashIndex >= 0
        ? normalizedPath.slice(0, lastSlashIndex + 1) + 'tonconnect-manifest.json'
        : '/tonconnect-manifest.json';
      var manifestUrl = new URL(manifestPath, window.location.href);
      manifestUrl.searchParams.set('v', manifestVersion);
      return manifestUrl.toString();
    } catch (e) {
      return '/tonconnect-manifest.json?v=' + encodeURIComponent(manifestVersion);
    }
  }

  function getTonConnectTwaReturnUrl() {
    var value = paymentSystemsOptions && paymentSystemsOptions.tonConnectTwaReturnUrl
      ? String(paymentSystemsOptions.tonConnectTwaReturnUrl).trim()
      : '';
    return value || '';
  }

  function applyTonConnectUiOptions() {
    if (!tonConnectUi) return;

    var nextOptions = {
      uiPreferences: { theme: 'DARK' }
    };

    var twaReturnUrl = getTonConnectTwaReturnUrl();
    if (twaReturnUrl) nextOptions.twaReturnUrl = twaReturnUrl;

    try {
      tonConnectUi.uiOptions = nextOptions;
    } catch (e) {
      console.warn('TON Connect UI options setup failed', e);
    }
  }

  function getTonConnectAccountAddress() {
    var account = null;
    if (tonConnectUi && tonConnectUi.account) account = tonConnectUi.account;
    if (!account && tonConnectWallet && tonConnectWallet.account) account = tonConnectWallet.account;
    if (!account && tonConnectUi && tonConnectUi.wallet && tonConnectUi.wallet.account) account = tonConnectUi.wallet.account;
    return account && account.address ? String(account.address).trim() : '';
  }

  function shortenWalletAddress(address) {
    var value = String(address || '').trim();
    if (value.length <= 16) return value;
    return value.slice(0, 6) + '…' + value.slice(-6);
  }

  function syncTonDepositScreenState() {
    if (!profileTonDepositScreenEl) return;

    var connectedAddress = getTonConnectAccountAddress();
    var hasTonDeposit = !!(activeDeposit && isTelegramTonMethod(activeDeposit.paymentMethod));
    profileTonDepositScreenEl.classList.toggle('profile-ton-screen-connected', !!connectedAddress && hasTonDeposit);
    profileTonDepositScreenEl.classList.toggle('profile-ton-screen-has-deposit', hasTonDeposit);
  }

  function syncDepositWalletButtonLabel() {
    if (!openDepositWalletBtn) return;
    if (activeDeposit && isTelegramTonMethod(activeDeposit.paymentMethod) && supportsTonConnect()) {
      openDepositWalletBtn.textContent = t('client.topup.tonConnect.payAction', 'Pay with TON Connect');
      return;
    }

    openDepositWalletBtn.textContent = t('client.topup.openWallet', 'Open wallet');
  }

  function renderTonConnectPanel() {
    if (!tonConnectPanelEl) return;

    var hasTonSystem = !!getPaymentSystemByKey('telegram_ton');
    var shouldShow = hasTonSystem && (activeProfileScreen === 'deposit-ton' || isTelegramTonMethod(getSelectedPaymentMethod()) || (activeDeposit && isTelegramTonMethod(activeDeposit.paymentMethod)));
    tonConnectPanelEl.hidden = !shouldShow;
    if (!shouldShow) {
      syncTonDepositScreenState();
      return;
    }

    var connectedAddress = getTonConnectAccountAddress();
    var available = supportsTonConnect();

    if (tonConnectStatusEl) {
      if (!available) {
        tonConnectStatusEl.textContent = t('client.topup.tonConnect.unavailable', 'TON Connect is unavailable here. You can still use the wallet links below.');
      } else if (connectedAddress) {
        tonConnectStatusEl.textContent = t('client.topup.tonConnect.connectedPrefix', 'Connected wallet: ') + shortenWalletAddress(connectedAddress);
      } else {
        tonConnectStatusEl.textContent = t('client.topup.tonConnect.notConnected', 'No TON wallet connected yet. You can connect now or when you start a payment.');
      }
    }

    if (tonConnectConnectBtn) tonConnectConnectBtn.hidden = !available || !!connectedAddress;
    if (tonConnectDisconnectBtn) tonConnectDisconnectBtn.hidden = !available || !connectedAddress;
    syncDepositWalletButtonLabel();
    syncTonDepositScreenState();
  }

  function ensureTonConnectUi() {
    if (tonConnectInitPromise) return tonConnectInitPromise;

    tonConnectInitPromise = Promise.resolve()
      .then(function () {
        if (!supportsTonConnect()) return null;
        if (tonConnectUi) return tonConnectUi;

        var ctor = window.TON_CONNECT_UI && window.TON_CONNECT_UI.TonConnectUI;
        if (typeof ctor !== 'function') return null;

        tonConnectUi = new ctor({
          manifestUrl: getTonConnectManifestUrl(),
          uiPreferences: { theme: 'DARK' }
        });
        applyTonConnectUiOptions();

        if (!tonConnectStatusUnsubscribe && typeof tonConnectUi.onStatusChange === 'function') {
          tonConnectStatusUnsubscribe = tonConnectUi.onStatusChange(function (wallet) {
            tonConnectWallet = wallet || null;
            renderTonConnectPanel();
          }, function (err) {
            console.warn('TON Connect status error', err);
          });
        }

        return Promise.resolve(tonConnectUi.connectionRestored)
          .catch(function () { return false; })
          .then(function () {
            tonConnectWallet = tonConnectUi.wallet || tonConnectWallet || null;
            renderTonConnectPanel();
            return tonConnectUi;
          });
      })
      .catch(function (err) {
        console.warn('TON Connect init failed', err);
        tonConnectUi = null;
        return null;
      });

    return tonConnectInitPromise;
  }

  function waitForTonConnectWallet(timeoutMs) {
    if (getTonConnectAccountAddress()) return Promise.resolve(true);

    return new Promise(function (resolve) {
      var done = false;
      var timerId = null;
      var pollId = null;
      var unsubscribe = null;

      function finish(result) {
        if (done) return;
        done = true;
        try { if (timerId) clearTimeout(timerId); } catch (e) { }
        try { if (pollId) clearInterval(pollId); } catch (e) { }
        try { if (typeof unsubscribe === 'function') unsubscribe(); } catch (e) { }
        resolve(result);
      }

      if (tonConnectUi && typeof tonConnectUi.onStatusChange === 'function') {
        unsubscribe = tonConnectUi.onStatusChange(function (wallet) {
          tonConnectWallet = wallet || null;
          if (getTonConnectAccountAddress()) finish(true);
        }, function () { });
      }

      pollId = setInterval(function () {
        if (getTonConnectAccountAddress()) finish(true);
      }, 500);
      timerId = setTimeout(function () { finish(false); }, Math.max(Number(timeoutMs) || 60000, 1000));
    });
  }

  function connectTonWallet() {
    return ensureTonConnectUi()
      .then(function (ui) {
        if (!ui || typeof ui.openModal !== 'function') {
          setTopUpStatus(t('client.topup.tonConnect.unavailable', 'TON Connect is unavailable here. You can still use the wallet links below.'));
          renderTonConnectPanel();
          return false;
        }

        setTopUpStatus(t('client.topup.tonConnect.connecting', 'Open your TON wallet and confirm the connection.'));
        return Promise.resolve(ui.openModal())
          .catch(function () { return null; })
          .then(function () { return waitForTonConnectWallet(60000); })
          .then(function (connected) {
            if (!connected) {
              setTopUpStatus(t('client.topup.tonConnect.cancelled', 'TON Connect was cancelled. You can retry or use the wallet links below.'));
              return false;
            }

            renderTonConnectPanel();
            return true;
          });
      });
  }

  function disconnectTonWallet() {
    ensureTonConnectUi()
      .then(function (ui) {
        if (!ui || typeof ui.disconnect !== 'function') return null;
        return ui.disconnect();
      })
      .catch(function () {
        return null;
      })
      .finally(function () {
        tonConnectWallet = null;
        renderTonConnectPanel();
        setTopUpStatus(t('client.topup.tonConnect.disconnected', 'TON wallet disconnected.'));
      });
  }

  function decimalToNanoString(value, decimals) {
    var normalized = String(value == null ? '' : value).trim();
    if (!/^\d+(\.\d+)?$/.test(normalized)) {
      var numeric = Number(value);
      if (!Number.isFinite(numeric) || numeric <= 0) return '';
      normalized = numeric.toFixed(decimals);
    }

    var parts = normalized.split('.');
    var whole = parts[0] || '0';
    var fraction = (parts[1] || '').slice(0, decimals);
    while (fraction.length < decimals) fraction += '0';
    var combined = (whole + fraction).replace(/^0+(?=\d)/, '');
    return combined || '0';
  }

  function buildTonConnectCommentPayload(comment) {
    var memo = String(comment || '').trim();
    if (!memo || !window.TonWeb || !TonWeb.boc || !TonWeb.utils) {
      return Promise.resolve(null);
    }

    try {
      var cell = new TonWeb.boc.Cell();
      cell.bits.writeUint(0, 32);
      cell.bits.writeString(memo);
      return Promise.resolve(cell.toBoc(false))
        .then(function (boc) {
          return TonWeb.utils.bytesToBase64(boc);
        })
        .catch(function () {
          return null;
        });
    } catch (e) {
      return Promise.resolve(null);
    }
  }

  function sendDepositViaTonConnect(deposit) {
    if (!deposit || !isTelegramTonMethod(deposit.paymentMethod) || !supportsTonConnect()) {
      return Promise.resolve('unavailable');
    }

    var address = String(deposit.destinationAddress || '').trim();
    var memo = String(deposit.destinationMemo || '').trim();
    var amount = decimalToNanoString(deposit.assetAmount, 9);
    if (!address || !memo || !amount) {
      return Promise.resolve('unavailable');
    }

    return ensureTonConnectUi()
      .then(function (ui) {
        if (!ui) return 'unavailable';

        var connectedAddress = getTonConnectAccountAddress();
        if (connectedAddress) return true;
        if (typeof ui.openModal !== 'function') return false;

        setTopUpStatus(t('client.topup.tonConnect.connecting', 'Open your TON wallet and confirm the connection.'));
        return Promise.resolve(ui.openModal())
          .catch(function () { return null; })
          .then(function () { return waitForTonConnectWallet(60000); });
      })
      .then(function (connected) {
        if (connected !== true) return connected === 'unavailable' ? connected : 'cancelled';
        return buildTonConnectCommentPayload(memo)
          .then(function (payload) {
            if (!payload) return 'unavailable';

            setTopUpStatus(t('client.topup.tonConnect.approve', 'Approve the TON transfer in your connected wallet.'));
            return tonConnectUi.sendTransaction({
              validUntil: Math.floor(Date.now() / 1000) + 15 * 60,
              messages: [{
                address: address,
                amount: amount,
                payload: payload
              }]
            })
              .then(function () { return 'sent'; })
              .catch(function () { return 'cancelled'; });
          });
      })
      .catch(function (err) {
        console.warn('TON Connect send failed', err);
        return 'unavailable';
      });
  }

  function syncTopUpButtonLabel() {
    if (!topUpBtn) return;
    topUpBtn.textContent = t('client.button.continue', 'Continue');
    topUpBtn.disabled = !getSelectedPaymentMethod();
  }

  function renderPaymentSystems() {
    if (!paymentSystemsListEl) return;

    var systems = getPaymentSystems();
    var selected = getSelectedPaymentMethod();
    paymentSystemsListEl.innerHTML = '';

    if (paymentSystemsHintEl) paymentSystemsHintEl.hidden = systems.length === 0;
    if (topUpBtn) topUpBtn.disabled = systems.length === 0;
    syncTopUpButtonLabel();
    renderTonConnectPanel();

    if (systems.length === 0) {
      var empty = document.createElement('div');
      empty.className = 'empty-card';
      empty.textContent = t('client.topup.noSystems', 'Crypto payments are not available right now.');
      paymentSystemsListEl.appendChild(empty);
      return;
    }

    systems.forEach(function (system) {
      var button = document.createElement('button');
      button.type = 'button';
      button.className = 'payment-system-icon-btn';
      if (selected === system.key) button.classList.add('payment-system-card-selected');
      button.setAttribute('aria-label', getPaymentMethodTitle(system.key));
      button.setAttribute('title', getPaymentMethodTitle(system.key));
      button.setAttribute('aria-pressed', selected === system.key ? 'true' : 'false');

      button.appendChild(createPaymentSystemIcon(system.key));

      var srText = document.createElement('span');
      srText.className = 'visually-hidden';
      srText.textContent = getPaymentMethodTitle(system.key);
      button.appendChild(srText);

      button.addEventListener('click', function () {
        setSelectedPaymentMethod(system.key);
      });

      paymentSystemsListEl.appendChild(button);
    });
  }

  function updateDepositDetails(deposit) {
    activeDeposit = deposit || null;
    if (!depositDetailsCardEl) return;

    var hasDeposit = !!deposit && isTelegramTonMethod(deposit.paymentMethod);
    depositDetailsCardEl.hidden = !hasDeposit;
    if (!hasDeposit) {
      syncTonDepositScreenState();
      return;
    }

    var displayAmount = formatCurrency(deposit.amount || 0);
    if (deposit.assetAmount && deposit.assetCode) {
      displayAmount = formatCryptoAssetAmount(deposit.assetAmount, deposit.assetCode) + ' • ' + formatCurrency(deposit.amount || 0);
    }

    if (depositDetailsAmountEl) depositDetailsAmountEl.textContent = displayAmount;
    if (depositDetailsAddressEl) depositDetailsAddressEl.textContent = String(deposit.destinationAddress || '-');
    if (depositDetailsMemoEl) depositDetailsMemoEl.textContent = String(deposit.destinationMemo || '-');
    if (depositDetailsTxEl) depositDetailsTxEl.textContent = String(deposit.providerTransactionId || '-');
    if (openDepositWalletBtn) openDepositWalletBtn.hidden = !String(deposit.checkoutLink || '').trim();
    if (openDepositAltBtn) openDepositAltBtn.hidden = !String(deposit.alternativeCheckoutLink || '').trim();
    if (copyDepositAddressBtn) copyDepositAddressBtn.hidden = !String(deposit.destinationAddress || '').trim();
    if (copyDepositMemoBtn) copyDepositMemoBtn.hidden = !String(deposit.destinationMemo || '').trim();
    if (copyDepositTxBtn) copyDepositTxBtn.hidden = !String(deposit.providerTransactionId || '').trim();
    if (checkDepositStatusBtn) checkDepositStatusBtn.hidden = !deposit.id;
    syncDepositWalletButtonLabel();
    renderTonConnectPanel();
    syncTonDepositScreenState();
  }

  function copyText(value, successMessage) {
    var text = String(value || '').trim();
    if (!text) {
      setTopUpStatus(t('client.topup.copyFailed', 'Copy failed.'));
      return Promise.resolve(false);
    }

    if (navigator && navigator.clipboard && navigator.clipboard.writeText) {
      return navigator.clipboard.writeText(text)
        .then(function () {
          setTopUpStatus(successMessage);
          return true;
        })
        .catch(function () {
          setTopUpStatus(t('client.topup.copyFailed', 'Copy failed.'));
          return false;
        });
    }

    setTopUpStatus(t('client.topup.copyFailed', 'Copy failed.'));
    return Promise.resolve(false);
  }

  function openActiveDepositLink(useAlternative) {
    if (!activeDeposit) return;
    if (!useAlternative && isTelegramTonMethod(activeDeposit.paymentMethod)) {
      sendDepositViaTonConnect(activeDeposit)
        .then(function (result) {
          if (result === 'sent') {
            setTopUpStatus(t('client.topup.tonConnect.sent', 'TON transaction was submitted. Waiting for blockchain confirmation.'));
            return;
          }

          if (result === 'cancelled') {
            setTopUpStatus(t('client.topup.tonConnect.cancelled', 'TON Connect was cancelled. You can retry or use the wallet links below.'));
            return;
          }

          var fallbackUrl = activeDeposit.checkoutLink;
          if (fallbackUrl) openCheckoutLink(fallbackUrl);
        });
      return;
    }

    var url = useAlternative ? activeDeposit.alternativeCheckoutLink : activeDeposit.checkoutLink;
    if (url) openCheckoutLink(url);
  }

  function checkActiveDepositStatus() {
    if (!activeDeposit || !activeDeposit.id) return;
    setTopUpStatus(t('client.topup.creatingInvoice', 'Preparing payment details...'));
    pollDepositStatus(activeDeposit.id, 1);
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
      var parts = [formatUtc(entry.createdAtUtc)];
      var provider = formatHistoryProvider(entry);
      if (provider) parts.push(provider);
      if (entry.assetAmount && entry.assetCode) {
        parts.push(t('client.history.assetPrefix', 'Asset:') + ' ' + formatCryptoAssetAmount(entry.assetAmount, entry.assetCode));
      }
      if (entry.externalId) {
        parts.push(t('client.history.externalIdPrefix', 'Reference:') + ' ' + entry.externalId);
      }
      sub.textContent = parts.join(' • ');

      row.appendChild(top);
      row.appendChild(amount);
      row.appendChild(sub);
      historyListEl.appendChild(row);
    });
  }

  function setProfileScreen(name) {
    var nextScreen = String(name || 'home').toLowerCase();
    if (nextScreen !== 'home' && nextScreen !== 'deposit' && nextScreen !== 'deposit-ton' && nextScreen !== 'invite' && nextScreen !== 'withdraw' && nextScreen !== 'history') {
      nextScreen = 'home';
    }

    activeProfileScreen = nextScreen;

    if (profileHomeScreenEl) profileHomeScreenEl.hidden = nextScreen !== 'home';
    if (profileDepositScreenEl) profileDepositScreenEl.hidden = nextScreen !== 'deposit';
    if (profileTonDepositScreenEl) profileTonDepositScreenEl.hidden = nextScreen !== 'deposit-ton';
    if (profileInviteScreenEl) profileInviteScreenEl.hidden = nextScreen !== 'invite';
    if (profileWithdrawScreenEl) profileWithdrawScreenEl.hidden = nextScreen !== 'withdraw';
    if (historyScreenEl) historyScreenEl.hidden = nextScreen !== 'history';

    if (nextScreen === 'history') {
      loadHistory();
    }

    if (nextScreen === 'deposit') {
      renderPaymentSystems();
      ensureTonConnectUi().finally(renderTonConnectPanel);
      updateDepositDetails(activeDeposit);
    }

    if (nextScreen === 'deposit-ton') {
      ensureTonConnectUi().finally(renderTonConnectPanel);
      updateDepositDetails(activeDeposit);
    }
  }

  function setActiveTab(name) {
    var tab = name || 'lottery';
    activeTabName = tab;
    var isLottery = tab === 'lottery';
    var isTickets = tab === 'tickets';
    var isWinners = tab === 'winners';
    var isProfile = tab === 'profile';

    if (lotteryTabPanel) lotteryTabPanel.hidden = !isLottery;
    if (ticketsTabPanel) ticketsTabPanel.hidden = !isTickets;
    if (winnersTabPanel) winnersTabPanel.hidden = !isWinners;
    if (profileTabPanel) profileTabPanel.hidden = !isProfile;

    if (lotteryTabBtn) lotteryTabBtn.classList.toggle('tabbar-btn-active', isLottery);
    if (ticketsTabBtn) ticketsTabBtn.classList.toggle('tabbar-btn-active', isTickets);
    if (winnersTabBtn) winnersTabBtn.classList.toggle('tabbar-btn-active', isWinners);
    if (profileTabBtn) profileTabBtn.classList.toggle('tabbar-btn-active', isProfile);

    if (isTickets) {
      renderMyTickets(latestState);
    }

    if (isWinners) {
      renderWinners(winnerEntries);
    }

    // Any footer tab click resets profile to its home menu.
    setProfileScreen('home');
  }

  function loadPublicMiniAppData() {
    return Promise.all([
      loadNewsBanners(),
      loadWinners()
    ]);
  }

  function loadPaymentSystems() {
    return getJson('/api/payments/systems')
      .then(function (res) {
        paymentSystemsOptions = res && res.ok && res.options ? res.options : { enabled: false, defaultPaymentMethod: null, systems: [] };
        if (!getPaymentSystemByKey(selectedPaymentMethod)) {
          selectedPaymentMethod = null;
        }
        applyTonConnectUiOptions();
        renderPaymentSystems();
        if (getPaymentSystemByKey('telegram_ton')) ensureTonConnectUi();
        return paymentSystemsOptions;
      })
      .catch(function () {
        paymentSystemsOptions = { enabled: false, defaultPaymentMethod: null, systems: [] };
        applyTonConnectUiOptions();
        renderPaymentSystems();
        return paymentSystemsOptions;
      });
  }

  function openTicketPurchaseScreen(drawId) {
    if (!ticketPurchaseScreenEl) return;

    purchaseScreenDrawId = drawId != null ? drawId : currentDisplayedDrawId;
    var draw = getPurchaseScreenDraw();
    if (!draw || !isDrawPurchasable(draw)) {
      setPurchaseStatus(getDrawUnavailableMessage(draw));
      return;
    }

    purchaseScreenSubmitting = false;
    ensurePurchaseScreenTicketStates();
    setActivePurchaseScreenTicket(0);
    setTicketPurchaseScreenStatus('');
    ticketPurchaseScreenEl.hidden = false;
    setSheetOpenClass();
    renderTicketPurchaseScreen();
  }

  function closeTicketPurchaseScreen() {
    if (!ticketPurchaseScreenEl) return;

    ticketPurchaseScreenEl.hidden = true;
    activePurchaseScreenTicketIndex = 0;
    purchaseScreenSubmitting = false;
    setTicketPurchaseScreenStatus('');
    syncTicketPurchaseFooterVisibility(false);
    setSheetOpenClass();
  }

  function getSessionRequiredMessage() {
    return t('client.status.authOpenFromTelegram', 'Authentication failed. Open this app from Telegram, or use the local debug profile in Development.');
  }

  function ensureSessionReadyForPurchase(statusSetter) {
    if (initData) return true;

    var message = getSessionRequiredMessage();
    if (typeof statusSetter === 'function') {
      statusSetter(message);
    }
    showCenterPopup(message);
    return false;
  }

  function submitTicketPurchase() {
    if (!ensureSessionReadyForPurchase(setTicketPurchaseScreenStatus)) return;

    var draw = getPurchaseScreenDraw();
    if (!draw || !isDrawPurchasable(draw)) {
      setTicketPurchaseScreenStatus(getDrawUnavailableMessage(draw));
      return;
    }

    var validation = getPurchaseScreenValidation();
    if (!validation.ok) {
      setTicketPurchaseScreenStatus(validation.message);
      showCenterPopup(validation.message);
      return;
    }

    var completedTickets = getCompletedPurchaseTickets().map(function (ticketState) {
      return normalizeTicketNumbers(ticketState.numbers);
    });

    purchaseScreenSubmitting = true;
    setTicketPurchaseScreenStatus(t('client.purchaseScreen.purchasing', 'Purchasing tickets...'));
    renderTicketPurchaseScreen();
    if (purchaseBtn) purchaseBtn.disabled = true;

    postJson('/api/tickets/purchase', {
      initData: initData || '',
      drawId: draw.id,
      tickets: completedTickets
    }, null)
      .then(function (res) {
        if (!(res && res.ok && Array.isArray(res.tickets))) {
          setTicketPurchaseScreenStatus(t('client.status.purchaseFailed', 'Purchase failed.'));
          return;
        }

        if (res.tickets.length > 0) {
          highlightTicketId = res.tickets[0].id;
        }
        setPurchaseStatus(formatPurchasedTicketsMessage(Number(res.purchasedCount || res.tickets.length || 0)));
        purchaseScreenTicketStates = [];
        ensurePurchaseScreenTicketStates();
        closeTicketPurchaseScreen();
        return refreshState();
      })
      .catch(function (err) {
        var message = err && err.message ? err.message : t('client.status.purchaseFailed', 'Purchase failed.');
        setTicketPurchaseScreenStatus(message);
        if (shouldShowInvalidTicketPopup(message)) {
          showCenterPopup(message);
        }
      })
      .finally(function () {
        purchaseScreenSubmitting = false;
        renderTicketPurchaseScreen();
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
            cardColor: state.currentDraw.cardColor || null,
          prizePool: state.currentDraw.prizePool,
          prizePoolMatch3: state.currentDraw.prizePoolMatch3,
          prizePoolMatch4: state.currentDraw.prizePoolMatch4,
          prizePoolMatch5: state.currentDraw.prizePoolMatch5,
          ticketCost: state.currentDraw.ticketCost,
          state: state.currentDraw.state,
          numbers: state.currentDraw.numbers || null,
          createdAtUtc: state.currentDraw.createdAtUtc || null,
          purchaseClosesAtUtc: state.currentDraw.purchaseClosesAtUtc || null,
          canPurchase: !!state.currentDraw.canPurchase
        } : null,
        activeDraws: (state && state.activeDraws || []).map(function (draw) {
          return {
            id: draw.id,
            cardColor: draw.cardColor || null,
            prizePool: draw.prizePool,
            prizePoolMatch3: draw.prizePoolMatch3,
            prizePoolMatch4: draw.prizePoolMatch4,
            prizePoolMatch5: draw.prizePoolMatch5,
            ticketCost: draw.ticketCost,
            state: draw.state,
            numbers: draw.numbers || null,
            createdAtUtc: draw.createdAtUtc || null,
            purchaseClosesAtUtc: draw.purchaseClosesAtUtc || null,
            canPurchase: !!draw.canPurchase
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
              cardColor: group.draw.cardColor || null,
              prizePool: group.draw.prizePool,
              state: group.draw.state,
              numbers: group.draw.numbers || null,
              createdAtUtc: group.draw.createdAtUtc || null,
              purchaseClosesAtUtc: group.draw.purchaseClosesAtUtc || null,
              canPurchase: !!group.draw.canPurchase
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
        }),
        ticketPurchase: state && state.ticketPurchase ? {
          ticketSlotsCount: Number(state.ticketPurchase.ticketSlotsCount || 0),
          numbersPerTicket: Number(state.ticketPurchase.numbersPerTicket || 0),
          minNumber: Number(state.ticketPurchase.minNumber || 0),
          maxNumber: Number(state.ticketPurchase.maxNumber || 0)
        } : null,
        balance: state && Number.isFinite(Number(state.balance)) ? Number(state.balance) : 0,
        serverNowUtc: state && state.serverNowUtc ? String(state.serverNowUtc) : null
      });
    } catch (e) {
      return String(Math.random());
    }
  }

  function applyState(state) {
    latestState = state || { balance: 0, serverNowUtc: null, currentDraw: null, activeDraws: [], activeTicketGroups: [], currentTickets: [], history: [], ticketPurchase: null };
    syncServerClock(latestState.serverNowUtc);

    var purchaseConfig = getTicketPurchaseConfig(latestState);
    if (purchaseConfig) {
      LOTTO_NUMBERS_COUNT = Math.max(1, parseInt(purchaseConfig.numbersPerTicket, 10) || LOTTO_NUMBERS_COUNT);
      LOTTO_MIN = Math.max(1, parseInt(purchaseConfig.minNumber, 10) || LOTTO_MIN);
      LOTTO_MAX = Math.max(LOTTO_MIN, parseInt(purchaseConfig.maxNumber, 10) || LOTTO_MAX);
      ticketSlotsPerPurchaseScreen = Math.max(1, parseInt(purchaseConfig.ticketSlotsCount, 10) || ticketSlotsPerPurchaseScreen);
      ensurePurchaseScreenTicketStates();
    }

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
    if (ticketPurchaseScreenEl && !ticketPurchaseScreenEl.hidden) {
      renderTicketPurchaseScreen();
    }

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

        syncServerClock(res.state.serverNowUtc);

        var sig = computeStateSig(res.state);
        if (sig !== lastStateSig) {
          lastStateSig = sig;
          applyState(res.state);
        } else {
          refreshCountdownVisuals();
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
        updateDepositDetails(deposit);
        var status = String(deposit.status || '').toLowerCase();
        if (status === 'credited') {
          setTopUpStatus(t('client.topup.creditedPrefix', 'Deposit credited: +') + formatCurrency(deposit.amount || 0) + '.');
          return refreshState()
            .then(function () { return loadHistory(); })
            .then(function () { return loadReferralProfile(); });
        }

        if (status === 'paid' || status === 'confirmed') {
          setTopUpStatus(t('client.topup.detected', 'Payment detected. Waiting for final credit...'));
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
    var isHttpUrl = /^https?:\/\//i.test(checkoutUrl);

    // Telegram openLink is best for normal https checkout pages, but custom wallet schemes like ton://
    // should fall back to direct navigation.
    try {
      if (isHttpUrl && window.Telegram && Telegram.WebApp && typeof Telegram.WebApp.openLink === 'function') {
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
    var paymentMethod = getSelectedPaymentMethod();
    if (!Number.isFinite(topUpAmount) || topUpAmount <= 0) {
      setTopUpStatus(t('client.topup.enterValidAmount', 'Enter a valid top up amount.'));
      return;
    }

    if (!paymentMethod) {
      setTopUpStatus(t('client.topup.systemUnavailable', 'Selected payment method is not available right now.'));
      return;
    }

    if (topUpBtn) topUpBtn.disabled = true;
    if (checkDepositStatusBtn) checkDepositStatusBtn.disabled = true;
    setTopUpStatus(t('client.topup.creatingInvoice', 'Creating crypto invoice...'));
    setWithdrawStatus('');

    postJson('/api/payments/deposits/create', { initData: initData || '', amount: topUpAmount, currency: 'USD', paymentMethod: paymentMethod }, null)
      .then(function (res) {
        if (!(res && res.ok && res.deposit)) {
          setTopUpStatus(t('client.topup.createFailed', 'Failed to create deposit invoice.'));
          return;
        }

        var deposit = res.deposit;
        updateDepositDetails(deposit);
        if (isTelegramTonMethod(paymentMethod)) {
          setProfileScreen('deposit-ton');
          return sendDepositViaTonConnect(deposit)
            .then(function (result) {
              if (result === 'sent') {
                setTopUpStatus(t('client.topup.tonConnect.sent', 'TON transaction was submitted. Waiting for blockchain confirmation.'));
              } else if (result === 'cancelled') {
                setTopUpStatus(t('client.topup.tonConnect.cancelled', 'TON Connect was cancelled. You can retry or use the wallet links below.'));
              } else {
                setTopUpStatus(t('client.topup.created.telegramTon', 'TON payment prepared. Open your wallet, send the exact amount, and keep the memo unchanged.'));
              }

              return pollDepositStatus(deposit.id, 45);
            });
        }

        if (deposit.checkoutLink) {
          openCheckoutLink(deposit.checkoutLink);
        }

        setTopUpStatus(t('client.topup.created.btcpay', 'BTCPay invoice created. Complete the payment in the opened checkout screen.'));
        return pollDepositStatus(deposit.id, 45);
      })
      .catch(function (err) {
        setTopUpStatus(err.message || t('client.topup.createFailed', 'Failed to create deposit invoice.'));
      })
      .finally(function () {
        if (topUpBtn) topUpBtn.disabled = false;
        if (checkDepositStatusBtn) checkDepositStatusBtn.disabled = false;
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
    if (!ensureSessionReadyForPurchase(setPurchaseStatus)) return;
    var selected = resolveSelectedDrawSnapshot(latestState);
    if (!selected.draw || !isDrawPurchasable(selected.draw)) {
      setPurchaseStatus(getDrawUnavailableMessage(selected.draw));
      return;
    }

    setPurchaseStatus('');
    // Ensure purchases from jackpot card target the featured/current draw
    if (currentDisplayedDrawId != null) selectedActiveDrawId = currentDisplayedDrawId;
    openTicketPurchaseScreen(currentDisplayedDrawId);
  }

  if (purchaseBtn) purchaseBtn.addEventListener('click', purchaseTicket);
  if (sortClosestDrawBtn) sortClosestDrawBtn.addEventListener('click', function () { setDrawSortMode('closest'); });
  if (sortBiggestJackpotBtn) sortBiggestJackpotBtn.addEventListener('click', function () { setDrawSortMode('jackpot'); });
  if (sortCheaperTicketsBtn) sortCheaperTicketsBtn.addEventListener('click', function () { setDrawSortMode('cheap'); });
  if (featuredJackpotCardEl) {
    featuredJackpotCardEl.addEventListener('click', function (event) {
      if (purchaseBtn && (event.target === purchaseBtn || purchaseBtn.contains(event.target))) {
        return;
      }

      purchaseTicket();
    });
  }
  if (lotteryTabBtn) lotteryTabBtn.addEventListener('click', function () { setActiveTab('lottery'); });
  if (ticketsTabBtn) ticketsTabBtn.addEventListener('click', function () { setActiveTab('tickets'); });
  if (winnersTabBtn) winnersTabBtn.addEventListener('click', function () { setActiveTab('winners'); });
  if (profileTabBtn) profileTabBtn.addEventListener('click', function () { setActiveTab('profile'); });
  if (closeTicketPurchaseScreenBtn) closeTicketPurchaseScreenBtn.addEventListener('click', closeTicketPurchaseScreen);
  if (submitTicketPurchaseBtn) submitTicketPurchaseBtn.addEventListener('click', submitTicketPurchase);
  if (centerPopupConfirmBtn) centerPopupConfirmBtn.addEventListener('click', hideCenterPopup);
  if (centerPopupBackdropEl) centerPopupBackdropEl.addEventListener('click', hideCenterPopup);
  // active draw selection handled via per-banner buy buttons
  if (topUpBtn) topUpBtn.addEventListener('click', topUpBalance);
  if (openDepositWalletBtn) openDepositWalletBtn.addEventListener('click', function () { openActiveDepositLink(false); });
  if (openDepositAltBtn) openDepositAltBtn.addEventListener('click', function () { openActiveDepositLink(true); });
  if (tonConnectConnectBtn) tonConnectConnectBtn.addEventListener('click', connectTonWallet);
  if (tonConnectDisconnectBtn) tonConnectDisconnectBtn.addEventListener('click', disconnectTonWallet);
  if (copyDepositAddressBtn) copyDepositAddressBtn.addEventListener('click', function () { copyText(activeDeposit && activeDeposit.destinationAddress, t('client.topup.addressCopied', 'Wallet address copied.')); });
  if (copyDepositMemoBtn) copyDepositMemoBtn.addEventListener('click', function () { copyText(activeDeposit && activeDeposit.destinationMemo, t('client.topup.memoCopied', 'Memo copied.')); });
  if (copyDepositTxBtn) copyDepositTxBtn.addEventListener('click', function () { copyText(activeDeposit && activeDeposit.providerTransactionId, t('client.topup.txCopied', 'Transaction id copied.')); });
  if (checkDepositStatusBtn) checkDepositStatusBtn.addEventListener('click', checkActiveDepositStatus);
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
  if (backFromTonDepositBtn) backFromTonDepositBtn.addEventListener('click', function () { setProfileScreen('deposit'); });
  if (backFromInviteBtn) backFromInviteBtn.addEventListener('click', function () { setProfileScreen('home'); });
  if (backFromWithdrawBtn) backFromWithdrawBtn.addEventListener('click', function () { setProfileScreen('home'); });
  if (closeHistoryBtn) closeHistoryBtn.addEventListener('click', function () { setProfileScreen('home'); });
  document.addEventListener('visibilitychange', function () {
    if (document.hidden) {
      stopNewsBannerAutoplay();
      return;
    }

    startNewsBannerAutoplay();
  });

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

  startCountdownTimer();
  ensurePurchaseScreenTicketStates();
  syncDrawSortTabs();
  setActiveTab('lottery');
  setProfileScreen('home');
  renderCurrentDraw(null, [], false);
  renderMyTickets({ balance: 0, currentDraw: null, activeDraws: [], activeTicketGroups: [], currentTickets: [], history: [], ticketPurchase: null });
  renderWinners([]);
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
      .then(loadPublicMiniAppData)
      .then(function () { return postJson('/api/auth/telegram', { initData: initData }); })
      .then(function () { return loadPaymentSystems(); })
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
      .then(loadPublicMiniAppData)
      .then(function () { return postJson('/api/auth/telegram', { initData: initData }); })
      .then(function () { return loadPaymentSystems(); })
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
