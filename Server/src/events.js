// Dunyodagi tadbirlar: har hafta takrorlanadigan jadval. Vaqtlar Toshkent vaqtida (UTC+5, yozgi vaqt yo'q),
// API esa ularni UTC (ISO) ko'rinishida qaytaradi.

const TASHKENT_OFFSET_MS = 5 * 60 * 60 * 1000;
const DAY_MS = 24 * 60 * 60 * 1000;

/** Zonalar (Unity'dagi LobbyZonesList va Loc.cs dagi "zone.*" kalitlari bilan bir xil). */
export const ZONES = ['entertainment', 'education', 'business', 'shops', 'community'];

const EVERY_DAY = [0, 1, 2, 3, 4, 5, 6];

/**
 * Haftalik jadval. days: 0 - yakshanba, 1 - dushanba, ..., 6 - shanba.
 * start/end: Toshkent vaqti "SS:DD"; end start'dan kichik bo'lsa tadbir ertasi kuni tugaydi.
 */
export const WEEKLY_SCHEDULE = [
  {
    id: 'concert',
    zone: 'entertainment',
    days: EVERY_DAY,
    start: '20:00',
    end: '22:00',
    title: { uz: 'Virtual konsert', en: 'Virtual Concert' },
    place: { uz: "Ko'ngilochar zona", en: 'Entertainment zone' },
  },
  {
    id: 'it-academy',
    zone: 'education',
    days: [2, 4],
    start: '18:00',
    end: '19:30',
    title: { uz: 'IT Academy: ochiq dars', en: 'IT Academy: open lesson' },
    place: { uz: "O'quv markazlari", en: 'Education centers' },
  },
  {
    id: 'startup-meetup',
    zone: 'business',
    days: [5],
    start: '19:00',
    end: '21:00',
    title: { uz: 'Startaplar uchrashuvi', en: 'Startup meetup' },
    place: { uz: 'Biznes markazi', en: 'Business center' },
  },
  {
    id: 'weekend-sale',
    zone: 'shops',
    days: [6],
    start: '12:00',
    end: '18:00',
    title: { uz: 'Dam olish kuni savdosi', en: 'Weekend sale' },
    place: { uz: 'Magazinlar', en: 'Shops' },
  },
  {
    id: 'new-friends-day',
    zone: 'community',
    days: [0],
    start: '16:00',
    end: '18:00',
    title: { uz: "Yangi do'stlar kuni", en: 'New friends day' },
    place: { uz: 'Hamjamiyat', en: 'Community' },
  },
];

/**
 * `now` dan keyingi `days` kun ichida boshlanadigan tadbirlar (hozir davom etayotganlari ham).
 * Tugagan tadbirlar kirmaydi; boshlanish vaqti bo'yicha tartiblangan, ko'pi bilan `limit` ta.
 * @returns {{ id: string, zone: string, title: { uz: string, en: string }, place: { uz: string, en: string },
 *             startsAt: string, endsAt: string }[]}
 */
export function upcomingEvents(now = new Date(), { days = 7, limit = 20, schedule = WEEKLY_SCHEDULE } = {}) {
  const nowMs = now.getTime();
  const horizonMs = nowMs + days * DAY_MS;

  // Toshkentdagi bugungi sananing boshi: "mahalliy vaqt" UTC soatlarida ifodalangan
  const local = new Date(nowMs + TASHKENT_OFFSET_MS);
  const todayLocal = Date.UTC(local.getUTCFullYear(), local.getUTCMonth(), local.getUTCDate());

  const events = [];
  // Kechagi kundan boshlaymiz: yarim tundan o'tadigan tadbir hali davom etayotgan bo'lishi mumkin
  for (let offset = -1; offset <= days; offset++) {
    const dayLocal = todayLocal + offset * DAY_MS;
    const weekday = new Date(dayLocal).getUTCDay();
    const date = new Date(dayLocal).toISOString().slice(0, 10);

    for (const item of schedule) {
      if (!item.days.includes(weekday)) continue;
      const startsAt = dayLocal + minutesOfDay(item.start) * 60_000 - TASHKENT_OFFSET_MS;
      let endsAt = dayLocal + minutesOfDay(item.end) * 60_000 - TASHKENT_OFFSET_MS;
      if (endsAt <= startsAt) endsAt += DAY_MS;
      if (endsAt <= nowMs || startsAt >= horizonMs) continue;

      events.push({
        id: `${item.id}-${date}`,
        zone: item.zone,
        title: { ...item.title },
        place: { ...item.place },
        startsAt: new Date(startsAt).toISOString(),
        endsAt: new Date(endsAt).toISOString(),
      });
    }
  }

  // ISO UTC satrlari bir xil uzunlikda, shuning uchun oddiy matn taqqoslash vaqt tartibini beradi
  events.sort((a, b) => compare(a.startsAt, b.startsAt) || compare(a.id, b.id));
  return events.slice(0, limit);
}

function compare(a, b) {
  return a < b ? -1 : a > b ? 1 : 0;
}

/** "20:30" -> 1230 (kun boshidan daqiqalar). */
function minutesOfDay(time) {
  const [hours, minutes] = time.split(':').map(Number);
  return hours * 60 + minutes;
}
