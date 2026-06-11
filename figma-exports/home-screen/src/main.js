import Vue from "vue";
import ElementHome from "./screens/ElementHome.vue";

Vue.config.productionTip = false;

function getTelegramInitData() {
  try {
    return window.Telegram && window.Telegram.WebApp && window.Telegram.WebApp.initData
      ? String(window.Telegram.WebApp.initData)
      : "";
  } catch (e) {
    return "";
  }
}

function resolveInitData() {
  var params = new URLSearchParams(window.location.search || "");
  var forceLocalDebug = params.get("debug") === "1" || params.get("mode") === "local-debug";
  var telegramInitData = getTelegramInitData();

  if (forceLocalDebug || !telegramInitData) {
    return { initData: "local-debug", isLocalDebug: true };
  }

  return { initData: telegramInitData, isLocalDebug: false };
}

function postJson(url, payload) {
  return fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload || {}),
  }).then(function (response) {
    return response.text().then(function (text) {
      var data = null;
      try {
        data = text ? JSON.parse(text) : null;
      } catch (e) {
        data = null;
      }

      if (!response.ok) {
        var message = data && (data.error || data.title || data.message)
          ? String(data.error || data.title || data.message)
          : "Request failed: " + response.status;
        throw new Error(message);
      }

      return data;
    });
  });
}

function tryPrepareTelegramShell() {
  try {
    if (window.Telegram && window.Telegram.WebApp) {
      window.Telegram.WebApp.ready();
      window.Telegram.WebApp.expand();
    }
  } catch (e) {
  }
}

new Vue({
  render: function (h) {
    return h(ElementHome, {
      props: {
        runtime: {
          initData: resolveInitData().initData,
          isLocalDebug: resolveInitData().isLocalDebug,
          postJson: postJson,
        },
      },
    });
  },
  created: function () {
    tryPrepareTelegramShell();
  },
}).$mount("#app");
