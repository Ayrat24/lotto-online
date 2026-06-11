# Prompt Template for Integrating New Figma Screens

Copy and customize this prompt when you add a new Figma-exported screen folder to the project.

---

## Prompt to Use

```
I have added a new Figma-exported screen to `figma-exports/<SCREEN-NAME>/`. 

Please integrate it into the MiniApp project following the patterns in `figma-exports/FIGMA_SCREENS_GUIDE.md` and the existing `figma-exports/home-screen/` implementation.

**Screen details:**
- Folder: `figma-exports/<SCREEN-NAME>/`
- Purpose: <DESCRIBE WHAT THIS SCREEN DOES>
- Backend APIs needed: <LIST APIs e.g. /api/timeline, /api/tickets, /api/winners>

**Tasks:**
1. Review the Anima-generated code in `figma-exports/<SCREEN-NAME>/`
2. Configure `vue.config.js` to output to `wwwroot/<SCREEN-NAME>-app/`
3. Update `main.js` with runtime integration (initData, postJson, Telegram shell)
4. Update the main screen component to:
   - Accept `runtime` prop
   - Load locale via `/api/localization/bootstrap`
   - Authenticate via `/api/auth/telegram`
   - Fetch screen-specific data from backend APIs
   - Handle loading/error states
5. Ensure `public/index.html` has proper fonts and viewport settings
6. Build and verify the output works at `/<SCREEN-NAME>-app/`

Follow Vue 2 syntax (no arrow functions in component options, use `var self = this` pattern).
```

---

## Example Prompts

### Tickets Screen
```
I have added a new Figma-exported screen to `figma-exports/tickets-screen/`. 

Please integrate it into the MiniApp project following the patterns in `figma-exports/FIGMA_SCREENS_GUIDE.md` and the existing `figma-exports/home-screen/` implementation.

**Screen details:**
- Folder: `figma-exports/tickets-screen/`
- Purpose: Display user's purchased tickets grouped by draw, with ticket status (active, won, lost)
- Backend APIs needed: /api/timeline (for tickets), /api/auth/telegram

**Tasks:**
1. Review the Anima-generated code in `figma-exports/tickets-screen/`
2. Configure `vue.config.js` to output to `wwwroot/tickets-screen-app/`
3. Update `main.js` with runtime integration (initData, postJson, Telegram shell)
4. Update the main screen component to:
   - Accept `runtime` prop
   - Load locale via `/api/localization/bootstrap`
   - Authenticate via `/api/auth/telegram`
   - Fetch tickets from `/api/timeline`
   - Group tickets by draw and show status
   - Handle loading/error states
5. Ensure `public/index.html` has proper fonts and viewport settings
6. Build and verify the output works at `/tickets-screen-app/`

Follow Vue 2 syntax (no arrow functions in component options, use `var self = this` pattern).
```

### Winners Screen
```
I have added a new Figma-exported screen to `figma-exports/winners-screen/`. 

Please integrate it into the MiniApp project following the patterns in `figma-exports/FIGMA_SCREENS_GUIDE.md` and the existing `figma-exports/home-screen/` implementation.

**Screen details:**
- Folder: `figma-exports/winners-screen/`
- Purpose: Show recent lottery winners with their names, winnings amounts, and quotes
- Backend APIs needed: /api/winners, /api/auth/telegram

**Tasks:**
1. Review the Anima-generated code in `figma-exports/winners-screen/`
2. Configure `vue.config.js` to output to `wwwroot/winners-screen-app/`
3. Update `main.js` with runtime integration (initData, postJson, Telegram shell)
4. Update the main screen component to:
   - Accept `runtime` prop
   - Load locale via `/api/localization/bootstrap`
   - Authenticate via `/api/auth/telegram`
   - Fetch winners list from `/api/winners`
   - Display winner cards with avatars, names, amounts, quotes
   - Handle loading/error states
5. Ensure `public/index.html` has proper fonts and viewport settings
6. Build and verify the output works at `/winners-screen-app/`

Follow Vue 2 syntax (no arrow functions in component options, use `var self = this` pattern).
```

### Profile Screen
```
I have added a new Figma-exported screen to `figma-exports/profile-screen/`. 

Please integrate it into the MiniApp project following the patterns in `figma-exports/FIGMA_SCREENS_GUIDE.md` and the existing `figma-exports/home-screen/` implementation.

**Screen details:**
- Folder: `figma-exports/profile-screen/`
- Purpose: User profile with balance, referral link, language settings, deposit/withdraw buttons
- Backend APIs needed: /api/auth/telegram, /api/wallet/balance, /api/referrals/link

**Tasks:**
1. Review the Anima-generated code in `figma-exports/profile-screen/`
2. Configure `vue.config.js` to output to `wwwroot/profile-screen-app/`
3. Update `main.js` with runtime integration (initData, postJson, Telegram shell)
4. Update the main screen component to:
   - Accept `runtime` prop
   - Load locale via `/api/localization/bootstrap`
   - Authenticate via `/api/auth/telegram`
   - Fetch balance from `/api/wallet/balance`
   - Fetch referral link from `/api/referrals/link`
   - Handle language switching
   - Handle loading/error states
5. Ensure `public/index.html` has proper fonts and viewport settings
6. Build and verify the output works at `/profile-screen-app/`

Follow Vue 2 syntax (no arrow functions in component options, use `var self = this` pattern).
```

---

## Quick Reference: Available APIs

| Endpoint | Purpose |
|----------|---------|
| `/api/localization/bootstrap` | Get/set user locale |
| `/api/auth/telegram` | Authenticate, get user profile |
| `/api/timeline` | Draws, tickets, balance |
| `/api/news-banners` | Promotional banners |
| `/api/tickets/purchase` | Purchase tickets |
| `/api/wallet/balance` | Get wallet balance |
| `/api/winners` | Winners list |
| `/api/referrals/link` | Get user's referral link |
| `/api/referrals/stats` | Referral statistics |
| `/api/payments/deposits/create` | Create crypto deposit |
| `/api/payments/withdrawals/create` | Create withdrawal |
