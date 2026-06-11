# Figma Screen Integration Guide

This guide explains how to add new screens exported from Figma to the MiniApp project. Follow these conventions to maintain consistency with the existing `home-screen` implementation.

---

## Overview

The project uses **Anima** (design-to-code platform) to export Figma designs as Vue 2 applications. Each screen is a standalone Vue CLI project in `figma-exports/<screen-name>/` that builds to `wwwroot/<screen-name>-app/`.

### Current Structure
```
figma-exports/
├── home-screen/           # Home screen Vue app (example)
│   ├── package.json
│   ├── vue.config.js
│   ├── .screen-graph.json
│   ├── public/
│   │   └── index.html
│   └── src/
│       ├── main.js
│       ├── components/    # Reusable components
│       └── screens/       # Screen components
│           └── ElementHome.vue
│           └── ElementHome/
│               └── sections/   # Screen subsections
└── FIGMA_SCREENS_GUIDE.md
```

---

## Step-by-Step: Adding a New Figma Screen

### 1. Export from Figma via Anima

1. Open your Figma design
2. Use Anima plugin to export as **Vue** project
3. Download the generated code package
4. Extract to `figma-exports/<screen-name>/` (e.g., `figma-exports/tickets-screen/`)

### 2. Configure vue.config.js

Create/update `figma-exports/<screen-name>/vue.config.js`:

```js
module.exports = {
  // URL path where the app will be served (must match ASP.NET route)
  publicPath: "/<screen-name>-app/",
  
  // Output directory relative to vue.config.js location
  outputDir: "../../wwwroot/<screen-name>-app",
  
  // Disable hash in filenames for predictable asset paths
  filenameHashing: false,
  
  // Put CSS/JS/images in assets/ subfolder
  assetsDir: "assets",
};
```

**Example for tickets-screen:**
```js
module.exports = {
  publicPath: "/tickets-screen-app/",
  outputDir: "../../wwwroot/tickets-screen-app",
  filenameHashing: false,
  assetsDir: "assets",
};
```

### 3. Update package.json

Ensure these scripts exist (Anima usually generates them):

```json
{
  "name": "anima-project",
  "version": "1.0.0",
  "type": "commonjs",
  "scripts": {
    "dev": "NODE_OPTIONS=--openssl-legacy-provider vue-cli-service serve",
    "build": "NODE_OPTIONS=--openssl-legacy-provider vue-cli-service build"
  },
  "dependencies": {
    "@vue/cli-plugin-babel": "4.1.1",
    "vue": "2.6.11",
    "vue-router": "3.6.5"
  },
  "devDependencies": {
    "@vue/cli-service": "4.1.1",
    "vue-template-compiler": "2.6.11"
  }
}
```

### 4. Configure main.js for Runtime Integration

Replace the Anima-generated `src/main.js` with this pattern that integrates with MiniApp backend:

```js
import Vue from "vue";
import ElementScreenName from "./screens/ElementScreenName.vue";  // Your main screen component

Vue.config.productionTip = false;

// Resolve Telegram initData or fall back to local-debug mode
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

// Helper for POST requests to backend APIs
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

// Initialize Telegram WebApp shell
function tryPrepareTelegramShell() {
  try {
    if (window.Telegram && window.Telegram.WebApp) {
      window.Telegram.WebApp.ready();
      window.Telegram.WebApp.expand();
    }
  } catch (e) {}
}

new Vue({
  render: function (h) {
    return h(ElementScreenName, {
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
```

### 5. Configure public/index.html

Ensure the HTML template includes Manrope font and proper viewport settings:

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>Screen Name - Lotto MiniApp</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <style>
    @import url("https://fonts.googleapis.com/css?family=Manrope:800,600,500,400,700");
  </style>
  <style>
    @import url("https://cdnjs.cloudflare.com/ajax/libs/meyer-reset/2.0/reset.min.css");
    * {
      -webkit-font-smoothing: antialiased;
      box-sizing: border-box;
    }
    html, body {
      margin: 0px;
      min-height: 100%;
    }
    button:focus-visible {
      outline: 2px solid #4a90e2 !important;
      outline: -webkit-focus-ring-color auto 5px !important;
    }
    a {
      text-decoration: none;
    }
  </style>
</head>
<body>
  <noscript>
    <strong>This app requires JavaScript to run.</strong>
  </noscript>
  <div id="app"></div>
  <!-- Vue CLI will inject built files here -->
</body>
</html>
```

### 6. Structure Main Screen Component

Your main screen component (e.g., `ElementTickets.vue`) should:

1. Accept `runtime` prop with `initData`, `isLocalDebug`, and `postJson`
2. Use computed properties for locale-aware formatting
3. Call backend APIs on mount
4. Implement polling for real-time updates if needed

**Template structure:**
```vue
<template>
  <div class="element-SCREENNAME" data-model-id="...">
    <!-- Sections go here -->
    <SectionOne :prop1="value1" />
    <SectionTwo :items="computedItems" />
    <TabBarSubsection />  <!-- If navigation is needed -->
  </div>
</template>

<script>
import SectionOne from "./ElementScreenName/sections/SectionOne.vue";
import SectionTwo from "./ElementScreenName/sections/SectionTwo.vue";
import TabBarSubsection from "./ElementScreenName/sections/TabBarSubsection.vue";

export default {
  name: "ElementScreenName",
  components: { SectionOne, SectionTwo, TabBarSubsection },
  props: {
    runtime: {
      type: Object,
      required: true,
    },
  },
  data: function () {
    return {
      initData: this.runtime.initData,
      isLocalDebug: !!this.runtime.isLocalDebug,
      locale: "en",
      loading: true,
      error: "",
      // Screen-specific data...
    };
  },
  computed: {
    // Locale-aware computed properties...
    intlLocale: function () {
      if (this.locale === "ru") return "ru-RU";
      if (this.locale === "uz") return "uz-UZ";
      return "en-US";
    },
  },
  methods: {
    // API calls using this.runtime.postJson(...)
    loadLocale: function () {
      var self = this;
      return this.runtime.postJson("/api/localization/bootstrap", { 
        initData: this.initData, 
        locale: this.locale 
      }).then(function (res) {
        if (res && res.ok) self.locale = String(res.locale || "en");
      });
    },
    loadAuth: function () {
      var self = this;
      return this.runtime.postJson("/api/auth/telegram", { initData: this.initData })
        .then(function (res) {
          if (res && res.ok) {
            // Store user info...
          }
        });
    },
    loadAll: function () {
      var self = this;
      this.loading = true;
      this.error = "";
      
      return this.loadLocale()
        .then(this.loadAuth)
        .then(function () {
          // Load screen-specific data...
        })
        .catch(function (err) {
          self.error = err && err.message ? err.message : "Failed to load.";
        })
        .finally(function () {
          self.loading = false;
        });
    },
  },
  mounted: function () {
    this.loadAll();
  },
};
</script>

<style>
/* Anima-generated styles */
</style>
```

### 7. Organize Sections

Place screen sections in `src/screens/ElementScreenName/sections/`:

```
src/screens/ElementScreenName/
├── ElementScreenName.vue        # Main screen
└── sections/
    ├── HeaderSubsection.vue
    ├── ContentSubsection.vue
    ├── TabBarSubsection.vue     # Can be shared across screens
    └── ComponentFolder/
        └── Component.vue
```

**Section component pattern:**
```vue
<template>
  <div class="section-name">
    <!-- Content with props -->
    <div class="text-wrapper">{{ title }}</div>
  </div>
</template>

<script>
export default {
  name: "SectionName",
  props: {
    title: { type: String, default: "" },
    // Other props...
  },
};
</script>

<style>
/* Anima-generated CSS */
</style>
```

---

## Building and Deploying

### Development (local preview)
```bash
cd figma-exports/<screen-name>
npm install
npm run dev
```

### Production Build
```bash
cd figma-exports/<screen-name>
npm install
npm run build
```

This outputs to `wwwroot/<screen-name>-app/` with:
```
wwwroot/<screen-name>-app/
├── index.html
├── assets/
│   ├── app.js
│   └── app.css
└── ...
```

### Serving via ASP.NET

The built files in `wwwroot/<screen-name>-app/` are automatically served as static files. Access at:
- `https://localhost:<port>/<screen-name>-app/`
- In production: `https://yourdomain.com/<screen-name>-app/`

Optionally create a Razor page route:

**Pages/ScreenName.cshtml:**
```razor
@page "/screen-name"
@{ Layout = null; }
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover" />
  <meta name="robots" content="noindex, nofollow" />
  <title>Screen Name - Lotto</title>
  <script src="https://telegram.org/js/telegram-web-app.js"></script>
</head>
<body>
  <div id="app"></div>
  <script src="/<screen-name>-app/assets/app.js"></script>
  <link rel="stylesheet" href="/<screen-name>-app/assets/app.css" />
</body>
</html>
```

---

## Available Backend APIs

These APIs are available for screen components to call:

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/localization/bootstrap` | POST | Get user locale, bootstrap localization |
| `/api/auth/telegram` | POST | Authenticate user, get profile info |
| `/api/timeline` | POST | Get draws, tickets, balance state |
| `/api/news-banners` | POST | Get promotional banners |
| `/api/tickets/purchase` | POST | Purchase tickets |
| `/api/wallet/balance` | POST | Get wallet balance |
| `/api/winners` | POST | Get winners list |

**Request pattern:**
```js
this.runtime.postJson("/api/<endpoint>", {
  initData: this.initData,  // Required for auth
  // Additional payload...
}).then(function (res) {
  if (res && res.ok) {
    // Handle success
  }
}).catch(function (err) {
  // Handle error
});
```

---

## Reusing Components Across Screens

### Shared Components Location
Place truly shared components in `figma-exports/shared-components/` or copy between screen folders.

### TabBar Navigation
The `TabBarSubsection.vue` can be copied between screens. Update the active tab state:

```vue
<!-- In TabBarSubsection.vue -->
<div :class="isActive('home') ? 'background-shadow-3' : 'container-11'">
  <!-- Home tab content -->
</div>
```

### Common Patterns to Copy
- `BrandRowWrapper.vue` - User info + balance header
- `TabBarSubsection.vue` - Bottom navigation
- Loading/error state displays

---

## Checklist for New Screen Integration

- [ ] Export Figma design via Anima to `figma-exports/<screen-name>/`
- [ ] Create `vue.config.js` with correct `publicPath` and `outputDir`
- [ ] Update `package.json` with proper scripts
- [ ] Configure `main.js` with runtime integration (initData, postJson)
- [ ] Update `public/index.html` with proper fonts and viewport
- [ ] Structure screen component with `runtime` prop
- [ ] Implement API calls for data loading (locale → auth → screen data)
- [ ] Add sections as child components
- [ ] Test locally with `npm run dev`
- [ ] Build for production with `npm run build`
- [ ] Verify output in `wwwroot/<screen-name>-app/`
- [ ] Test in browser at `/<screen-name>-app/`
- [ ] Test with `?debug=1` for local debug mode
- [ ] Optionally create Razor page route

---

## Troubleshooting

### Build fails with OpenSSL error
Ensure `NODE_OPTIONS=--openssl-legacy-provider` is in the npm scripts (required for Node 17+).

### Fonts not loading
Check that Google Fonts import is in `public/index.html`:
```html
<style>
  @import url("https://fonts.googleapis.com/css?family=Manrope:800,600,500,400,700");
</style>
```

### API calls fail in local development
Use `?debug=1` or `?mode=local-debug` query param to force local debug mode, which sends `initData: "local-debug"` to backend.

### Styles not applying
Ensure `filenameHashing: false` in `vue.config.js` and CSS reset is imported.

### Component width issues
Anima exports often use fixed widths (e.g., `width: 390px`). Add `max-width: 100%` for responsiveness:
```css
.element-SCREENNAME .container {
  width: 390px;
  max-width: 100%;
}
```
