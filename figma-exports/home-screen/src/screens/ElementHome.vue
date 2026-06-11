<template>
<div class="element-HOME" data-model-id="1:3">
<div class="container-16">
<BrandRowSubsection
  :avatar-letter="avatarLetter"
  :display-name="displayName"
  :subtitle="userSubtitle"
  :balance-label="texts.balanceLabel"
  :balance-text="formattedBalance"
/>
<div class="news">
<div class="text-wrapper-27">
{{ texts.newsTitle }}
        </div>
</div>
<MarginSubsection
  :badge-text="texts.bannerBadge"
  :title="bannerTitle"
  :subtitle="bannerSubtitle"
  :image-url="bannerImageUrl"
/>
<SegmentedMarginSubsection
  :options="sortOptions"
  :selected-value="sortMode"
  @change="handleSortChange"
/>
<DrawCardsSubsection
  :draws="formattedDraws"
  :loading="loading"
  :error="error"
  :loading-text="texts.loadingText"
  :empty-text="texts.emptyDrawsText"
  :jackpot-label="texts.jackpotLabel"
  :ticket-price-label="texts.ticketPriceLabel"
/>
<ContainerSubsection
  :title="texts.offersTitle"
  :action-text="texts.seeAllText"
/>
<ContainerWrapperSubsection
  :primary-offer="primaryOffer"
  :secondary-offer="secondaryOffer"
/>
</div>
<TabBarSubsection
  :home-text="texts.homeTab"
  :tickets-text="texts.ticketsTab"
  :winners-text="texts.winnersTab"
  :profile-text="texts.profileTab"
/>
</div>
</template>
<script>
import BrandRowSubsection from "./ElementHome/sections/BrandRowSubsection/BrandRowSubsection.vue";
import ContainerSubsection from "./ElementHome/sections/ContainerSubsection/ContainerSubsection.vue";
import ContainerWrapperSubsection from "./ElementHome/sections/ContainerWrapperSubsection.vue";
import DrawCardsSubsection from "./ElementHome/sections/DrawCardsSubsection/DrawCardsSubsection.vue";
import MarginSubsection from "./ElementHome/sections/MarginSubsection/MarginSubsection.vue";
import SegmentedMarginSubsection from "./ElementHome/sections/SegmentedMarginSubsection/SegmentedMarginSubsection.vue";
import TabBarSubsection from "./ElementHome/sections/TabBarSubsection.vue";

export default {
  name: "ElementHome",
  components: {
    BrandRowSubsection,
    ContainerSubsection,
    ContainerWrapperSubsection,
    DrawCardsSubsection,
    MarginSubsection,
    SegmentedMarginSubsection,
    TabBarSubsection,
  },
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
      user: { firstName: "Player", lastName: "", username: "", balance: 0 },
      timeline: null,
      banners: [],
      loading: true,
      error: "",
      sortMode: "closest",
      countdownIntervalId: null,
      refreshIntervalId: null,
      texts: {
        newsTitle: "Последние новости",
        bannerBadge: "АНОНС",
        balanceLabel: "БАЛАНС",
        jackpotLabel: "ДЖЕКПОТ",
        ticketPriceLabel: "ЦЕНА БИЛЕТА",
        loadingText: "Loading home screen…",
        emptyDrawsText: "There are no active draws right now.",
        offersTitle: "Сегодняшние предложения",
        seeAllText: "Смотреть все",
        homeTab: "Главная",
        ticketsTab: "Мои билеты",
        winnersTab: "Победители",
        profileTab: "Профиль",
        closestSort: "Ближайший тираж",
        jackpotSort: "Высокий суперприз",
        cheapSort: "Сначала дешевле",
      },
    };
  },
  computed: {
    intlLocale: function () {
      if (this.locale === "ru") return "ru-RU";
      if (this.locale === "uz") return "uz-UZ";
      return "en-US";
    },
    displayName: function () {
      var name = [this.user.firstName, this.user.lastName].filter(Boolean).join(" ").trim();
      return name || this.user.username || "Player";
    },
    avatarLetter: function () {
      return (this.displayName || "P").slice(0, 1).toUpperCase();
    },
    userSubtitle: function () {
      return "3 билета в игре · 1 выигрыш";
    },
    formattedBalance: function () {
      return this.formatCurrency(this.user.balance, 2).replace('.', ',');
    },
    sortOptions: function () {
      return [
        { value: "closest", label: this.texts.closestSort },
        { value: "jackpot", label: this.texts.jackpotSort },
        { value: "cheap", label: this.texts.cheapSort },
      ];
    },
    activeDraws: function () {
      var draws = this.timeline && Array.isArray(this.timeline.activeDraws) ? this.timeline.activeDraws.slice() : [];
      return draws.filter(function (draw) {
        return draw && draw.state === "active" && draw.canPurchase !== false;
      }).sort(this.compareDraws(this.sortMode));
    },
    formattedDraws: function () {
      var self = this;
      return this.activeDraws.map(function (draw, index) {
        var color = String((draw && draw.cardColor) || "gold").toLowerCase();
        return {
          id: draw.id,
          title: "Тираж #" + draw.id,
          countdown: self.formatCountdown(draw.purchaseClosesAtUtc),
          jackpot: self.formatJackpot(draw.prizePoolMatch5),
          ticketPrice: self.formatCurrency(draw.ticketCost, 2),
          theme: color === "blue" || index % 2 === 1 ? "blue" : "orange",
        };
      });
    },
    featuredBanner: function () {
      return this.banners && this.banners.length ? this.banners[0] : null;
    },
    bannerImageUrl: function () {
      return this.featuredBanner && this.featuredBanner.imageUrl ? this.featuredBanner.imageUrl : "";
    },
    bannerTitle: function () {
      return this.featuredBanner && (this.featuredBanner.title || this.featuredBanner.name)
        ? String(this.featuredBanner.title || this.featuredBanner.name)
        : "Не знаешь, как получить крипту?";
    },
    bannerSubtitle: function () {
      return this.featuredBanner && (this.featuredBanner.subtitle || this.featuredBanner.description)
        ? String(this.featuredBanner.subtitle || this.featuredBanner.description)
        : "Переходи по ссылке в боте";
    },
    primaryOffer: function () {
      return {
        kicker: "БОНУС НОВИЧКА",
        title: "3 бесплатных билета",
        actionText: "Получить",
      };
    },
    secondaryOffer: function () {
      return {
        kicker: "ПРИГЛАШАЙ И ЗАРАБАТЫВАЙ",
        title: "$5 за каждого друга",
        actionText: "Поделиться",
      };
    },
  },
  methods: {
    handleSortChange: function (value) {
      this.sortMode = value || "closest";
    },
    formatCurrency: function (value, digits) {
      var amount = Number(value || 0);
      if (!Number.isFinite(amount)) amount = 0;
      return "$" + amount.toLocaleString(this.intlLocale, {
        minimumFractionDigits: digits == null ? 2 : digits,
        maximumFractionDigits: digits == null ? 2 : digits,
      });
    },
    formatJackpot: function (value) {
      var amount = Number(value || 0);
      if (!Number.isFinite(amount)) amount = 0;
      return "$" + Math.round(amount).toLocaleString(this.intlLocale);
    },
    formatCountdown: function (targetUtc) {
      var targetMs = Date.parse(targetUtc || "");
      if (!Number.isFinite(targetMs)) return "Schedule pending";

      var remaining = Math.max(0, Math.floor((targetMs - Date.now()) / 1000));
      var hours = Math.floor(remaining / 3600);
      var minutes = Math.floor((remaining % 3600) / 60);
      var seconds = remaining % 60;

      if (hours > 0) {
        return String(hours).padStart(2, "0") + ":" + String(minutes).padStart(2, "0") + ":" + String(seconds).padStart(2, "0");
      }

      return String(minutes).padStart(2, "0") + ":" + String(seconds).padStart(2, "0");
    },
    compareDraws: function (mode) {
      return function (a, b) {
        var aClose = Date.parse((a && a.purchaseClosesAtUtc) || "") || Number.MAX_SAFE_INTEGER;
        var bClose = Date.parse((b && b.purchaseClosesAtUtc) || "") || Number.MAX_SAFE_INTEGER;
        var aJackpot = Number((a && a.prizePoolMatch5) || 0);
        var bJackpot = Number((b && b.prizePoolMatch5) || 0);
        var aCost = Number((a && a.ticketCost) || 0);
        var bCost = Number((b && b.ticketCost) || 0);
        var aId = Number((a && a.id) || 0);
        var bId = Number((b && b.id) || 0);

        if (mode === "jackpot") return (bJackpot - aJackpot) || (aClose - bClose) || (aCost - bCost) || (bId - aId);
        if (mode === "cheap") return (aCost - bCost) || (bJackpot - aJackpot) || (aClose - bClose) || (bId - aId);
        return (aClose - bClose) || (bJackpot - aJackpot) || (aCost - bCost) || (bId - aId);
      };
    },
    loadLocale: function () {
      var self = this;
      return this.runtime.postJson("/api/localization/bootstrap", { initData: this.initData, locale: this.locale })
        .then(function (res) {
          if (res && res.ok) self.locale = String(res.locale || "en");
        });
    },
    loadAuth: function () {
      var self = this;
      return this.runtime.postJson("/api/auth/telegram", { initData: this.initData })
        .then(function (res) {
          if (res && res.ok) {
            self.user.firstName = res.firstName || "";
            self.user.lastName = res.lastName || "";
            self.user.username = res.username || "";
            self.user.balance = Number(res.balance || 0);
          }
        });
    },
    loadTimeline: function () {
      var self = this;
      return this.runtime.postJson("/api/timeline", { initData: this.initData })
        .then(function (res) {
          if (res && res.ok) {
            self.timeline = res.state;
            self.user.balance = Number((res.state && res.state.balance) || self.user.balance || 0);
          }
        });
    },
    loadBanners: function () {
      var self = this;
      return this.runtime.postJson("/api/news-banners", { initData: this.initData, locale: this.locale })
        .then(function (res) {
          self.banners = res && res.ok && Array.isArray(res.banners) ? res.banners : [];
        });
    },
    loadAll: function () {
      var self = this;
      this.error = "";
      this.loading = true;

      return this.loadLocale()
        .then(this.loadAuth)
        .then(function () {
          return Promise.all([self.loadTimeline(), self.loadBanners()]);
        })
        .catch(function (error) {
          self.error = error && error.message ? error.message : "Failed to load home screen.";
        })
        .finally(function () {
          self.loading = false;
        });
    },
  },
  mounted: function () {
    var self = this;
    this.loadAll();
    this.countdownIntervalId = window.setInterval(function () {
      self.$forceUpdate();
    }, 1000);
    this.refreshIntervalId = window.setInterval(function () {
      self.loadTimeline().catch(function () {});
      self.loadBanners().catch(function () {});
    }, 4000);
  },
  beforeDestroy: function () {
    if (this.countdownIntervalId) window.clearInterval(this.countdownIntervalId);
    if (this.refreshIntervalId) window.clearInterval(this.refreshIntervalId);
  },
};
</script>
<style>
.element-HOME {
  align-items: center;
  background-color: #ffffff;
  display: inline-flex;
  flex-direction: column;
  gap: 14px;
  min-height: 100vh;
  padding: 18px 0px 0px;
  position: relative;
  width: 100%;
}

.element-HOME .container-16 {
  align-items: flex-start;
  display: flex;
  flex: 1;
  flex-direction: column;
  flex-grow: 1;
  overflow-y: auto;
  padding: 0px 0px 328px;
  position: relative;
  width: 390px;
  max-width: 100%;
}

.element-HOME .container-16::-webkit-scrollbar {
  display: none;
  width: 0;
}

.element-HOME .news {
  align-items: flex-start;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding: 0px 20px 10px;
  position: relative;
  width: 390px;
  max-width: 100%;
}

.element-HOME .text-wrapper-27 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 18px;
  font-weight: 700;
  letter-spacing: -0.3px;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  width: fit-content;
}
</style>