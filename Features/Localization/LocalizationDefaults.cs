namespace MiniApp.Features.Localization;

internal static class LocalizationDefaults
{
    public static readonly IReadOnlyDictionary<string, (string En, string Ru, string Uz)> Entries
        = new Dictionary<string, (string En, string Ru, string Uz)>(StringComparer.Ordinal)
        {
            ["client.tab.home"] = ("Home", "Glavnaya", "Bosh sahifa"),
            ["client.tab.tickets"] = ("My Tickets", "Moi bilety", "Mening biletlarim"),
            ["client.tab.profile"] = ("Profile", "Profil", "Profil"),
            ["client.balance.label"] = ("Money", "Balans", "Balans"),
            ["client.button.purchase"] = ("Purchase ticket", "Kupit bilet", "Bilet sotib olish"),
            ["client.button.deposit"] = ("Deposit crypto", "Popolnit crypto", "Kripto toldirish"),
            ["client.button.withdraw"] = ("Withdraw", "Vyvesti", "Yechib olish"),
            ["client.button.saveWallet"] = ("Save wallet address", "Sohranit adres", "Hamyon manzilini saqlash"),
            ["client.status.loadingHistory"] = ("Loading transaction history...", "Zagruzka istorii tranzaktsiy...", "Tranzaksiya tarixi yuklanmoqda..."),
            ["client.status.noActiveDraw"] = ("There is no active draw right now.", "Seychas net aktivnogo tirazha.", "Hozir faol tiraj yoq."),
            ["client.status.ticketPurchased"] = ("Ticket purchased.", "Bilet kuplen.", "Bilet sotib olindi."),
            ["client.status.purchaseFailed"] = ("Purchase failed.", "Pokupka ne udalas.", "Xarid muvaffaqiyatsiz."),
            ["client.status.authenticationFailed"] = ("Authentication failed.", "Oshibka avtorizatsii.", "Autentifikatsiya muvaffaqiyatsiz."),
            ["client.picker.preparing"] = ("Preparing numbers...", "Podgotovka chisel...", "Raqamlar tayyorlanmoqda..."),
            ["client.picker.chooseUnique"] = ("Please choose 5 unique numbers.", "Vyberite 5 unikalnyh chisel.", "Iltimos, 5 ta noyob raqam tanlang."),
            ["client.picker.submitting"] = ("Submitting ticket...", "Otpravka bileta...", "Bilet yuborilmoqda..."),
            ["client.picker.confirm"] = ("Confirm numbers", "Podtverdit chisla", "Raqamlarni tasdiqlash"),
            ["admin.nav.localization"] = ("Localization", "Lokalizatsiya", "Lokalizatsiya"),
            ["admin.localization.title"] = ("Localization", "Lokalizatsiya", "Lokalizatsiya"),
            ["admin.localization.subtitle"] = ("Update en / ru / uz UI strings.", "Obnovite stroki en / ru / uz.", "en / ru / uz UI matnlarini yangilang."),
            ["bot.welcomeBack"] = ("Welcome back!", "S vozvrashcheniem!", "Xush kelibsiz!"),
            ["bot.openMiniApp"] = ("Open Mini App", "Otkryt Mini App", "Mini Appni ochish"),
            ["bot.changeLanguage"] = ("Change language", "Smenit yazyk", "Tilni ozgartirish"),
            ["bot.tapOpenMiniApp"] = ("Tap the button below to open the Mini App.", "Nazhmi knopku nizhe chtoby otkryt Mini App.", "Mini Appni ochish uchun pastdagi tugmani bosing."),
            ["bot.askLanguage"] = ("Please choose your language:", "Pozhaluysta vyberite yazyk:", "Iltimos, tilni tanlang:"),
            ["bot.askContact"] = ("Please share your contact to continue.", "Pozhaluysta otpravte kontakt chtoby prodolzhit.", "Davom etish uchun kontakt yuboring."),
            ["bot.shareContact"] = ("Share my contact", "Podelitsya kontaktom", "Kontaktni yuborish"),
            ["bot.savedNumber"] = ("Thanks! Saved.", "Spasibo! Sohraneno.", "Rahmat! Saqlandi."),
            ["bot.invalidNumber"] = ("I could not read a valid phone number. Please tap 'Share my contact' or send the number as text.", "Ne udalos raspoznat nomer. Nazhmite 'Podelitsya kontaktom' ili otpravte nomer tekstom.", "Raqamni oqib bolmadi. 'Kontaktni yuborish' tugmasini bosing yoki raqamni matn qilib yuboring."),
            ["bot.languageUpdated"] = ("Language updated.", "Yazyk obnovlen.", "Til yangilandi."),
            ["bot.languageBeforePhone"] = ("First choose a language.", "Snachala vyberite yazyk.", "Avval tilni tanlang."),
            ["bot.askPhonePlaceholder"] = ("Tap the button to share your phone number", "Nazhmi knopku chtoby podelitsya nomerom", "Telefon raqamingizni yuborish uchun tugmani bosing")
        };
}

