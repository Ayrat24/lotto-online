<template>
  <div class="element-HOME" data-model-id="1:132">
    <div class="container-7">
      <BrandRowSubsection
        :avatar-letter="avatarLetter"
        :display-name="displayName"
        :subtitle="userSubtitle"
        :balance-label="texts.balanceLabel"
        :balance-text="formattedBalance"
      />
      <div class="container-8">
        <img
          class="background-border"
          alt="Background border"
          src="https://c.animaapp.com/c5LmgKAe/img/background-border-shadow.svg"
        />
        <div class="container-9">
          <div class="text-wrapper-15">{{ texts.title }}</div>
        </div>
      </div>

      <div v-if="loading" class="state-message">
        {{ texts.loadingText }}
      </div>
      <div v-else-if="error" class="state-message state-message--error">
        {{ error }}
      </div>
      <div v-else class="tickets-list">
        <TicketSubsection
          v-for="(ticket, index) in ticketEntries"
          :key="ticket.id"
          :ticket-number="index + 1"
          :numbers-per-ticket="ticketPurchase.numbersPerTicket"
          :min-number="ticketPurchase.minNumber"
          :max-number="ticketPurchase.maxNumber"
          :ticket-cost="ticket.ticketCost"
          :selected-count="ticket.selectedNumbers.length"
          :selected-total="ticket.selectedNumbers"
          :can-purchase="ticket.selectedNumbers.length === ticketPurchase.numbersPerTicket"
          :selected="ticket.selectedNumbers"
          @update:selected="updateTicketSelection(ticket.id, $event)"
          @randomize="randomizeTicket(ticket.id)"
          @clear="clearTicket(ticket.id)"
          @purchase="purchaseTicket(ticket.id)"
        />
      </div>

      <ContainerSubsection />
      <TicketWrapperSubsection
        v-if="showPurchaseBar"
        :total-cost="selectedTicketsTotalCost"
        :purchase-text="texts.purchaseText"
        @purchase="purchaseSelectedTickets"
      />
    </div>
    <TabBarSubsection />
  </div>
</template>
<script>
import BrandRowSubsection from "./sections/BrandRowSubsection/BrandRowSubsection.vue";
import ContainerSubsection from "./sections/ContainerSubsection.vue";
import TabBarSubsection from "./sections/TabBarSubsection.vue";
import TicketSubsection from "./sections/TicketSubsection/TicketSubsection.vue";
import TicketWrapperSubsection from "./sections/TicketWrapperSubsection/TicketWrapperSubsection.vue";

export default {
  name: "ElementHome",
  components: {
    BrandRowSubsection,
    ContainerSubsection,
    TabBarSubsection,
    TicketSubsection,
    TicketWrapperSubsection,
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
      loading: true,
      error: "",
      user: {
        firstName: "",
        lastName: "",
        username: "",
        balance: 0,
      },
      draws: [],
      ticketPurchase: {
        ticketSlotsCount: 1,
        numbersPerTicket: 5,
        minNumber: 1,
        maxNumber: 36,
      },
      ticketEntries: [],
      texts: {
        title: "Выберите билеты",
        loadingText: "Loading ticket selection…",
        purchaseText: "Purchase",
        balanceLabel: "BALANCE",
        ticketsTab: "Tickets",
      },
    };
  },
  computed: {
    displayName: function () {
      var name = [this.user.firstName, this.user.lastName].filter(Boolean).join(" ").trim();
      return name || this.user.username || "Player";
    },
    avatarLetter: function () {
      return (this.displayName || "P").slice(0, 1).toUpperCase();
    },
    userSubtitle: function () {
      return "Select tickets for the active draw";
    },
    formattedBalance: function () {
      return this.formatCurrency(this.user.balance);
    },
    showPurchaseBar: function () {
      return this.ticketEntries.some(function (ticket) {
        return ticket.selectedNumbers.length === ticket.numbersPerTicket;
      });
    },
    selectedTicketsTotalCost: function () {
      return this.ticketEntries.reduce(function (sum, ticket) {
        return sum + (ticket.selectedNumbers.length === ticket.numbersPerTicket ? ticket.ticketCost : 0);
      }, 0);
    },
    activeDraw: function () {
      var active = this.draws.filter(function (draw) {
        return draw && draw.state === "active" && draw.canPurchase !== false;
      });
      return active.length ? active[0] : null;
    },
  },
  methods: {
    formatCurrency: function (value) {
      var amount = Number(value || 0);
      if (!Number.isFinite(amount)) amount = 0;
      return "$" + amount.toLocaleString(this.locale === "ru" ? "ru-RU" : this.locale === "uz" ? "uz-UZ" : "en-US", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      });
    },
    randomSelection: function () {
      var numbers = [];
      var used = {};
      while (numbers.length < this.ticketPurchase.numbersPerTicket) {
        var value = Math.floor(Math.random() * (this.ticketPurchase.maxNumber - this.ticketPurchase.minNumber + 1)) + this.ticketPurchase.minNumber;
        if (!used[value]) {
          used[value] = true;
          numbers.push(value);
        }
      }
      numbers.sort(function (a, b) {
        return a - b;
      });
      return numbers;
    },
    createEmptyTicketEntries: function () {
      var count = Math.max(1, Number((this.ticketPurchase && this.ticketPurchase.ticketSlotsCount) || 1));
      var entries = [];
      for (var i = 0; i < count; i += 1) {
        entries.push({
          id: "ticket-" + (i + 1),
          ticketNumber: i + 1,
          ticketCost: Number(this.activeDraw && this.activeDraw.ticketCost ? this.activeDraw.ticketCost : 0),
          numbersPerTicket: Number(this.ticketPurchase.numbersPerTicket || 5),
          selectedNumbers: [],
        });
      }
      this.ticketEntries = entries;
    },
    updateTicketSelection: function (ticketId, numbers) {
      var self = this;
      this.ticketEntries = this.ticketEntries.map(function (ticket) {
        if (ticket.id !== ticketId) return ticket;
        return {
          id: ticket.id,
          ticketNumber: ticket.ticketNumber,
          ticketCost: ticket.ticketCost,
          numbersPerTicket: ticket.numbersPerTicket,
          selectedNumbers: Array.isArray(numbers) ? numbers.slice(0, ticket.numbersPerTicket) : [],
        };
      });
      return self.ticketEntries;
    },
    randomizeTicket: function (ticketId) {
      this.updateTicketSelection(ticketId, this.randomSelection());
    },
    clearTicket: function (ticketId) {
      this.updateTicketSelection(ticketId, []);
    },
    purchaseTicket: function (ticketId) {
      var ticket = this.ticketEntries.find(function (item) {
        return item.id === ticketId;
      });
      if (!ticket || ticket.selectedNumbers.length !== ticket.numbersPerTicket) return;
      return this.purchaseSelectedTickets([ticket.selectedNumbers]);
    },
    purchaseSelectedTickets: function (forcedTickets) {
      var self = this;
      var tickets = forcedTickets || this.ticketEntries
        .filter(function (ticket) {
          return ticket.selectedNumbers.length === ticket.numbersPerTicket;
        })
        .map(function (ticket) {
          return ticket.selectedNumbers;
        });

      if (!tickets.length || !this.activeDraw) return Promise.resolve();

      this.loading = true;
      this.error = "";

      return this.runtime.postJson("/api/tickets/purchase", {
        initData: this.initData,
        drawId: this.activeDraw.id,
        tickets: tickets,
      })
        .then(function (res) {
          if (res && res.ok) {
            self.user.balance = Number(res.balance || self.user.balance || 0);
            self.createEmptyTicketEntries();
          }
        })
        .catch(function (error) {
          self.error = error && error.message ? error.message : "Purchase failed.";
        })
        .finally(function () {
          self.loading = false;
        });
    },
    loadLocale: function () {
      var self = this;
      return this.runtime.postJson("/api/localization/bootstrap", {
        initData: this.initData,
        locale: this.locale,
      }).then(function (res) {
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
            self.draws = res.state && Array.isArray(res.state.activeDraws) ? res.state.activeDraws : [];
            if (res.state && res.state.ticketPurchase) {
              self.ticketPurchase = {
                ticketSlotsCount: Number(res.state.ticketPurchase.ticketSlotsCount || 1),
                numbersPerTicket: Number(res.state.ticketPurchase.numbersPerTicket || 5),
                minNumber: Number(res.state.ticketPurchase.minNumber || 1),
                maxNumber: Number(res.state.ticketPurchase.maxNumber || 36),
              };
            }
            self.createEmptyTicketEntries();
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
          return self.loadTimeline();
        })
        .catch(function (error) {
          self.error = error && error.message ? error.message : "Failed to load ticket selection.";
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
.element-HOME {
  align-items: center;
  background-color: #ffffff;
  border-radius: 27px;
  display: inline-flex;
  flex-direction: column;
  gap: 14px;
  height: 840px;
  overflow: hidden;
  padding: 18px 0px 0px;
  position: relative;
}

.element-HOME .container-7 {
  align-items: center;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  gap: 14px;
  overflow-y: scroll;
  position: relative;
  width: 390px;
}

.element-HOME .container-7::-webkit-scrollbar {
  display: none;
  width: 0;
}

.element-HOME .container-8 {
  align-items: center;
  align-self: stretch;
  display: flex;
  flex: 0 0 auto;
  gap: 12px;
  padding: 0px 20px;
  position: relative;
  width: 100%;
}

.element-HOME .background-border {
  height: 40px;
  position: relative;
  width: 40px;
}

.element-HOME .container-9 {
  align-items: flex-start;
  display: inline-flex;
  flex: 0 0 auto;
  flex-direction: column;
  position: relative;
}

.element-HOME .text-wrapper-15 {
  align-items: center;
  color: #0f0f12;
  display: flex;
  font-family: "Manrope", Helvetica;
  font-size: 24px;
  font-weight: 800;
  letter-spacing: -0.5px;
  line-height: normal;
  margin-top: -1px;
  position: relative;
  width: fit-content;
}
</style>