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

  document.getElementById('closeBtn').addEventListener('click', function () {
    Telegram.WebApp.close();
  });
})();
