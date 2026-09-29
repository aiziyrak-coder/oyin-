using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CraDev.Online;
using UnityEngine;
using UnityEngine.UI;

namespace CraDev.MainMenu
{
    // Bosh sahifadagi "Keyingi tadbir" kartasi: serverdagi haqiqiy jadval (GET /api/events), aloqa bo'lmasa qayta
    // urinish, sana va vaqt mahalliy ko'rinishda. Bosilsa tadbir haqida batafsil oyna yoki aniq xabar chiqadi.
    public partial class LobbyContent
    {
        [Tooltip("Tadbir kartasi: bosilsa batafsil oyna.")]
        [SerializeField] Button eventCard;
        [Tooltip("Kartadagi asosiy yozuv: tadbir nomi (tadbir bo'lmasa \"Keyingi tadbir\").")]
        [SerializeField] Text eventName;
        bool eventsAnswered, eventsFailed, eventsRetry, eventCardBound;
        const float EventRefresh = 60, EventRetry = 15;

        // Arxiv sahifalari rasmlarini bo'shatishda (LobbyPage.ReleaseArchivedArt) ta'lim va do'stlar rasmlari ham hisoblanadi.
        protected override IEnumerable<Texture> ArtTextures()
        {
            foreach (var texture in base.ArtTextures()) yield return texture;
            yield return educationImage;
            yield return friendsImage;
        }

        /// <summary>Kartada haqiqiy (hali tugamagan) tadbir bormi.</summary>
        public bool HasNextEvent => nextEvent != null && nextEvent.EndsLocal > DateTime.Now;

        IEnumerator EventLoop()
        {
            if (!eventCardBound && eventCard != null) { eventCardBound = true; eventCard.onClick.AddListener(ShowEventDetails); }
            while (true)
            {
                RefreshEventCard();
                ApiResult<EventList> result = default;
                yield return Lobby.Api.Events(r => result = r);
                eventsAnswered = true;
                var now = DateTime.Now;
                float wait = EventRetry;
                if (result.Ok)
                {
                    eventsFailed = false;
                    nextEvent = result.Data.items?.FirstOrDefault(e => e?.title != null && e.EndsLocal > now);
                    wait = EventRefresh;
                    // Tadbir boshlanishi yoki tugashi bilan ro'yxat darhol yangilanadi
                    if (nextEvent != null)
                        wait = Mathf.Clamp((float)((nextEvent.StartsLocal > now ? nextEvent.StartsLocal : nextEvent.EndsLocal) - now).TotalSeconds + 1, 5, EventRefresh);
                }
                else eventsFailed = true; // oxirgi ma'lum tadbir (hali tugamagan bo'lsa) kartada qoladi
                RefreshEventCard();
                // Kutish paytida ham "Bugun/Ertaga/Hozir" yozuvi eskirmaydi; bosilganda oflayn bo'lsa darhol qayta so'raladi
                float until = Time.realtimeSinceStartup + wait, label = Time.realtimeSinceStartup + 20;
                while (Time.realtimeSinceStartup < until && !eventsRetry)
                {
                    yield return new WaitForSecondsRealtime(.5f);
                    if (Time.realtimeSinceStartup >= label) { label = Time.realtimeSinceStartup + 20; RefreshEventCard(); }
                }
                eventsRetry = false;
            }
        }

        void RefreshEventCard()
        {
            if (eventTitle == null) return;
            var now = DateTime.Now;
            bool live = HasNextEvent;
            if (eventName != null) eventName.text = live ? nextEvent.title.Value : Loc.T("home.event_next");
            eventTitle.text = live ? When(nextEvent, now, false)
                : Loc.T(eventsFailed ? "lobby.event.offline" : eventsAnswered ? "lobby.event.none" : "common.connecting");
        }

        void ShowEventDetails()
        {
            if (ModalWindow.AnyOpen || Lobby == null) return;
            var now = DateTime.Now;
            if (HasNextEvent)
            {
                string place = nextEvent.place?.Value;
                string status = nextEvent.StartsLocal <= now ? Loc.T("lobby.event.live_note") : Loc.F("lobby.event.starts_in", Duration(nextEvent.StartsLocal - now));
                string message = (string.IsNullOrEmpty(place) ? "" : place + "\n") + When(nextEvent, now, true) + "\n" + status;
                Lobby.Dialog.Show(nextEvent.title.Value, message, Loc.T("common.ok"), null);
                return;
            }
            if (eventsFailed) { Lobby.Toast(Loc.T("menu.offline")); eventsRetry = true; return; }
            Lobby.Toast(Loc.T(eventsAnswered ? "friends.events_empty" : "common.connecting"));
        }

        // "Bugun · 20:00", "Ertaga · 20:00", "Payshanba · 18:00", "05.10 · 18:00" yoki "Hozir · 22:00 gacha".
        // full: kun yonida sana va tugash vaqti ham ("Payshanba, 02.10 · 18:00–19:30").
        static string When(GameEvent evt, DateTime now, bool full)
        {
            DateTime start = evt.StartsLocal, end = evt.EndsLocal;
            if (start <= now && now < end) return Loc.F("lobby.event.live", Clock(end));
            int days = (start.Date - now.Date).Days;
            string date = start.ToString("dd.MM", CultureInfo.InvariantCulture);
            string day = days == 0 ? Loc.T("lobby.event.today") : days == 1 ? Loc.T("lobby.event.tomorrow")
                : days > 1 && days < 7 ? Loc.T("lobby.weekday." + (int)start.DayOfWeek) : date;
            if (full && days >= 0 && days < 7) day += ", " + date;
            return day + " · " + Clock(start) + (full && end > start ? "–" + Clock(end) : "");
        }

        static string Clock(DateTime time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

        static string Duration(TimeSpan span)
        {
            int minutes = Mathf.Max(1, (int)Math.Ceiling(span.TotalMinutes));
            if (minutes < 60) return Loc.F("lobby.event.minutes", minutes);
            int hours = minutes / 60; minutes %= 60;
            if (hours >= 48) return Loc.F("lobby.event.days", hours / 24);
            return minutes == 0 ? Loc.F("lobby.event.hours", hours) : Loc.F("lobby.event.hours_minutes", hours, minutes);
        }
    }
}
