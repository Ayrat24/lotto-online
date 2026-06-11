# Figma Screen Integration Guide

This guide explains how to add new screens exported from Figma to the MiniApp project. Follow these conventions to maintain consistency with the existing implementation.

---

## Overview

The project uses a **Single Page Application (SPA) architecture** for the Mini App. All screens are rendered within a persistent App Shell that provides:

1. **Fixed Header** - Always visible at the top (user avatar, name, subtitle, balance)
2. **Scrollable Content** - Screen-specific content that changes based on navigation
3. **Fixed Tab Bar** - Always visible at the bottom (navigation tabs)

This ensures consistent navigation and user context across all screens without page reloads.

---

## App Shell Architecture (MANDATORY)

**All screens MUST be rendered within the App Shell.** The header and tab bar are persistent - they render once and stay visible while only the content area changes.

### Layout Structure

```
┌─────────────────────────────────────┐
│       AppHeader (position: fixed)   │  ← z-index: 90
│  [Avatar] [Name + Subtitle] [Balance]│
├─────────────────────────────────────┤
│         Header Spacer (82px)        │  ← Pushes content below fixed header
├─────────────────────────────────────┤
│                                     │
│      Screen Content (scrollable)    │  ← Changes based on currentScreen
│                                     │
│                                     │
├─────────────────────────────────────┤
│        AppTabBar (position: fixed)  │  ← z-index: 100
│  [Home] [Tickets] [Winners] [Profile]│
└─────────────────────────────────────┘
```

### Key CSS Classes

```css
/* App Shell container */
.app-shell {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  width: 100%;
  max-width: 390px;
  margin: 0 auto;
}

/* Fixed header */
.app-header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 90;
  background: #ffffff;
  height: ~82px;
  max-width: 390px;
  margin: 0 auto;
}

/* Header spacer - offsets content below fixed header */
.header-spacer {
  height: 82px;
  flex-shrink: 0;
}

/* Screen content areas */
.home-content,
.ticket-selection-content,
.your-screen-content {
  flex: 1;
  padding-bottom: 100px; /* Space for tab bar */
  overflow-y: auto;
}

/* Fixed tab bar */
.tab-bar-subsection {
  position: fixed;
  bottom: 16px;
  left: 14px;
  right: 14px;
  height: 68px;
  z-index: 100;
  max-width: 390px;
}
```

---

## Current Project Structure

```
wwwroot/dist/
├── assets/
│   ├── index.js          # Main SPA JavaScript (Vue 3)
│   └── index.css         # All styles including App Shell
figma-exports/
├── home-screen/          # Figma export (design reference only)
├── FIGMA_SCREENS_GUIDE.md
└── PROMPT_TEMPLATE.md
```

The main SPA code is in `wwwroot/dist/assets/index.js`. This file contains:
- Vue 3 reactive state management
- All screen components (Home, Ticket Selection, etc.)
- Navigation logic via `state.currentScreen`
- API integration for backend calls

---

## Adding a New Screen

### Step 1: Add Screen State

In `wwwroot/dist/assets/index.js`, add your screen to the navigation:

```javascript
// In getInitialScreen() function
function getInitialScreen() {
  const params = new URLSearchParams(window.location.search || '');
  const screen = params.get('screen');
  if (screen === 'ticket-selection') return 'ticket-selection';
  if (screen === 'your-screen') return 'your-screen';  // Add this
  return 'home';
}
```

### Step 2: Add Screen Template

In the App template, add your screen with `v-else-if`:

```html
<!-- Home Screen -->
<div v-if="state.currentScreen === 'home'" class="home-content">
  <!-- Home content -->
</div>

<!-- Ticket Selection Screen -->
<div v-else-if="state.currentScreen === 'ticket-selection'" class="ticket-selection-content">
  <!-- Ticket selection content -->
</div>

<!-- Your New Screen -->
<div v-else-if="state.currentScreen === 'your-screen'" class="your-screen-content">
  <!-- Your screen content here -->
  <!-- DO NOT include header or tab bar - App Shell handles this -->
</div>
```

### Step 3: Add Navigation Function

```javascript
function openYourScreen(params) {
  state.currentScreen = 'your-screen';
  state.yourScreenData = params;  // Store any needed data
  updateUrl();
}
```

### Step 4: Add CSS Styles

In `wwwroot/dist/assets/index.css`:

```css
/* Your Screen */
.your-screen-content {
  flex: 1;
  padding: 0 20px 120px;  /* Bottom padding for tab bar */
  overflow-y: auto;
}

/* Your screen-specific styles */
.your-component {
  /* styles */
}
```

### Step 5: Update URL Handling

In `updateUrl()` function:

```javascript
function updateUrl() {
  const url = new URL(window.location.href);
  if (state.currentScreen === 'ticket-selection' && state.selectedDrawId) {
    url.searchParams.set('screen', 'ticket-selection');
    url.searchParams.set('drawId', String(state.selectedDrawId));
  } else if (state.currentScreen === 'your-screen') {
    url.searchParams.set('screen', 'your-screen');
    // Add any screen-specific params
  } else {
    url.searchParams.delete('screen');
    url.searchParams.delete('drawId');
  }
  window.history.replaceState({}, '', url.toString());
}
```

---

## Screen Navigation

Navigation is handled via `state.currentScreen`:

| Screen Value | Description | Tab Highlight |
|--------------|-------------|---------------|
| `'home'` | Main screen with draws | Home |
| `'ticket-selection'` | Number selection for tickets | Home |
| `'tickets'` | My purchased tickets | Tickets |
| `'winners'` | Winners list | Winners |
| `'profile'` | User profile | Profile |

For sub-screens (like ticket selection), the active tab remains the same as the parent screen.

---

## Header Subtitle Context

The header subtitle changes based on the current screen:

```javascript
// In template
<p class="app-header__subtitle">
  {{ state.currentScreen === 'ticket-selection' 
     ? 'Выберите номера для билета' 
     : '3 билета в игре · 1 выигрыш' }}
</p>
```

Update this logic when adding new screens to show context-appropriate subtitles.

---

## Available Backend APIs

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/localization/bootstrap` | POST | Get user locale |
| `/api/auth/telegram` | POST | Authenticate user |
| `/api/timeline` | POST | Get draws, tickets, balance |
| `/api/news-banners` | POST | Get promotional banners |
| `/api/tickets/purchase` | POST | Purchase tickets |
| `/api/wallet/balance` | POST | Get wallet balance |
| `/api/winners` | POST | Get winners list |

**Request pattern:**
```javascript
const res = await postJson('/api/<endpoint>', {
  initData: state.initData,  // Required for auth
  // Additional payload...
});
if (res && res.ok) {
  // Handle success
}
```

---

## Checklist for New Screen Integration

- [ ] Add screen value to `getInitialScreen()` URL parsing
- [ ] Add screen template with `v-else-if` in App template
- [ ] Create navigation function (e.g., `openYourScreen()`)
- [ ] Add CSS class `.your-screen-content` with proper padding
- [ ] Update `updateUrl()` for URL state management
- [ ] Update header subtitle logic if needed
- [ ] Add any computed properties for screen data
- [ ] Test navigation from other screens
- [ ] Test direct URL access (e.g., `?screen=your-screen`)
- [ ] Verify header and tab bar remain visible
- [ ] Verify content scrolls properly with fixed elements

---

## Design Reference: Figma Exports

Figma exports in `figma-exports/` are used as **design references only**. They are not built or deployed separately.

When implementing a new screen:
1. Export from Figma via Anima to `figma-exports/<screen-name>/`
2. Use the exported CSS and HTML as reference
3. Implement the screen directly in `wwwroot/dist/assets/index.js` and `index.css`
4. Do NOT create separate standalone apps

---

## Troubleshooting

### Content hidden behind header
Add `.header-spacer` div after the header, or ensure your content container has proper top padding.

### Content hidden behind tab bar
Ensure your screen content has `padding-bottom: 100px` or more.

### Screen not showing
1. Check `state.currentScreen` value in browser console
2. Verify `v-else-if` condition matches exactly
3. Check for JavaScript errors in console

### Navigation not working
1. Verify navigation function updates `state.currentScreen`
2. Check that `updateUrl()` is called
3. Verify the screen template exists

### Styles not applying
1. Check CSS class names match between JS and CSS
2. Verify CSS is in `wwwroot/dist/assets/index.css`
3. Check for CSS specificity conflicts

---

## Critical CSS Rules (MUST FOLLOW)

These rules prevent common issues that cause screens to appear blank or broken:

### 1. Never Use Fixed Heights on Screen Containers

**❌ BAD - Causes blank screens:**
```css
.my-screen {
  height: 840px;      /* Fixed height clips content */
  overflow: hidden;   /* Hides overflowing content */
}
```

**✅ GOOD - Flexible layout:**
```css
.my-screen {
  min-height: 100%;   /* Grows with content */
  display: flex;
  flex-direction: column;
}
```

### 2. Use Unique, Scoped Class Names

**❌ BAD - Generic names cause conflicts:**
```css
.background-border { ... }  /* May conflict with other screens */
.container { ... }          /* Too generic */
```

**✅ GOOD - Screen-prefixed names:**
```css
.ts-back-btn { ... }        /* ts = ticket-selection */
.hs-banner { ... }          /* hs = home-screen */
```

### 3. Always Use `scoped` Styles in Vue Components

```vue
<style scoped>
/* Styles only apply to this component */
.my-class { ... }
</style>
```

### 4. Ensure Proper Scrolling

```css
/* Screen content should scroll, not be clipped */
.screen-content {
  flex: 1;
  overflow-y: auto;
  padding-bottom: 100px; /* Space for tab bar */
}
```

### 5. Test Both Data States

Always verify screens work with:
- **Data present**: Normal rendering
- **Data missing/null**: Loading states, error messages, empty states

Add console logging during development:
```javascript
console.log('[ScreenName] data:', { prop1, prop2 })
```

---

## Figma-to-Code Translation Guide

When implementing Figma designs:

| Figma Property | CSS Equivalent |
|----------------|----------------|
| `layout: column` | `display: flex; flex-direction: column;` |
| `layout: row` | `display: flex; flex-direction: row;` |
| `gap: 14px` | `gap: 14px;` |
| `padding: 22px` | `padding: 22px;` |
| `borderRadius: 22px` | `border-radius: 22px;` |
| `fill: #FFFFFF` | `background: #FFFFFF;` |
| `stroke: #E7E7E7` | `border: 1px solid #E7E7E7;` |
| `effects: boxShadow` | `box-shadow: ...;` |
| `sizing: fill` | `flex: 1;` or `width: 100%;` |
| `sizing: hug` | `width: fit-content;` |

### Common Figma Colors in This Project

| Color | Usage |
|-------|-------|
| `#0F0F12` | Primary text |
| `#1A1C1E` | Dark text |
| `#6C727A` | Secondary text |
| `#8A8A8A` | Muted text |
| `#E7E7E7` | Borders |
| `#FAFAF7` | Light backgrounds |
| `#FFB929` | Primary accent (yellow) |
| `#1AA873` | Success/balance (green) |

### Common Figma Fonts

| Style | CSS |
|-------|-----|
| Title | `font-family: 'Manrope'; font-weight: 800; font-size: 24px;` |
| Subtitle | `font-family: 'Manrope'; font-weight: 700; font-size: 18px;` |
| Body | `font-family: 'Manrope'; font-weight: 500; font-size: 14px;` |
| Caption | `font-family: 'Manrope'; font-weight: 400; font-size: 12px;` |
| Button | `font-family: 'Manrope'; font-weight: 700; font-size: 13px; text-transform: uppercase;` |
