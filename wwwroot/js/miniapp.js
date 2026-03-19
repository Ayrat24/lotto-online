(function () {
  var infoEl = document.getElementById('info');

  // Always try to load backend-controlled text (works both in normal browser and Telegram)
  fetch('/api/text')
    .then(function (r) { return r.ok ? r.json() : Promise.reject(new Error('HTTP ' + r.status)); })
    .then(function (data) {
      if (data && typeof data.text === 'string') {
        infoEl.textContent = data.text;
      }
    })
    .catch(function (err) {
      // Don't fail the whole app if API isn't reachable
      console.warn('Failed to load /api/text', err);
    });

  if (!window.Telegram || !Telegram.WebApp) {
    // Not inside Telegram; keep the page usable in a normal browser.
    return;
  }

  Telegram.WebApp.ready();
  Telegram.WebApp.expand();

  // Secure login: send initData to backend for validation and user upsert.
  try {
    var initData = Telegram.WebApp.initData;
    if (initData && initData.length > 0) {
      fetch('/api/auth/telegram', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ initData: initData })
      })
        .then(function (r) {
          if (!r.ok) return Promise.reject(new Error('Auth failed HTTP ' + r.status));
          return r.json();
        })
        .then(function (res) {
          // Optional UI feedback
          if (res && res.ok && res.telegramUserId) {
            console.log('Authenticated Telegram user:', res.telegramUserId);
          }
        })
        .catch(function (err) {
          console.warn('Failed to auth with Telegram initData', err);
        });
    }
  } catch (e) {
    console.warn('Telegram auth initData error', e);
  }

  document.getElementById('closeBtn').addEventListener('click', function () {
    Telegram.WebApp.close();
  });
})();
