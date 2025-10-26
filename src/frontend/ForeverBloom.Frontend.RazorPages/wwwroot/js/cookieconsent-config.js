import 'https://cdn.jsdelivr.net/gh/orestbida/cookieconsent@3.1.0/dist/cookieconsent.umd.js';
import { trackPageView } from './analytics.js';

CookieConsent.run({
    cookie: {
        name: 'cc_cookie_foreverbloom',
        expiresAfterDays: 182,
    },

    guiOptions: {
        consentModal: {
            layout: 'cloud inline',
            position: 'bottom right',
            equalWeightButtons: true,
            flipButtons: false
        },
        preferencesModal: {
            layout: 'box',
            equalWeightButtons: true,
            flipButtons: false
        }
    },

    onConsent: ({cookie}) => {
        const userGaveConsent = cookie.categories.includes('analytics');

        // Only track the page view if:
        // 1. Analytics is enabled for this environment (googleAnalyticsSettings exists)
        // 2. The user has actually given consent for the analytics category
        if (window.googleAnalyticsSettings && window.googleAnalyticsSettings.trackingId && userGaveConsent) {
            trackPageView();
        }
    },

    categories: {
        necessary: {
            enabled: true,  // this category is required and cannot be disabled
            readOnly: true
        },
        analytics: {
            autoClear: {
                cookies: [
                    {
                        name: /^_ga/,   // regex: match all cookies starting with '_ga'
                    },
                    {
                        name: '_gid',   // string: exact cookie name
                    }
                ]
            },

            services: {
                ga: {
                    label: 'Google Analytics',
                    onAccept: () => {
                        if (typeof gtag === 'function') {
                            gtag('consent', 'update', {
                                'analytics_storage': 'granted'
                            });
                        }
                    },
                    onReject: () => {
                        if (typeof gtag === 'function') {
                            gtag('consent', 'update', {
                                'analytics_storage': 'denied'
                            });
                        }
                    }
                }
            }
        }
    },

    language: {
        default: 'pl',
        translations: {
            pl: {
                consentModal: {
                    title: 'Używamy plików cookie',
                    description: 'Ta strona korzysta z plików cookie, aby zapewnić Ci najlepszą jakość jej użytkowania. Wybierając „Akceptuj wszystko”, zgadzasz się na używanie przez nas plików cookie do celów analitycznych.',
                    acceptAllBtn: 'Akceptuj wszystko',
                    acceptNecessaryBtn: 'Odrzuć wszystko',
                    showPreferencesBtn: 'Zarządzaj preferencjami',
                    footer: `
                        <a href="/polityka-prywatnosci" target="_blank">Polityka prywatności</a>
                    `
                },
                preferencesModal: {
                    title: 'Zarządzaj preferencjami cookies',
                    acceptAllBtn: 'Akceptuj wszystko',
                    acceptNecessaryBtn: 'Odrzuć wszystko',
                    savePreferencesBtn: 'Zapisz preferencje',
                    closeIconLabel: 'Zamknij okno',
                    serviceCounterLabel: 'Usługa|Usługi',
                    sections: [
                        {
                            title: 'Twoje wybory dotyczące prywatności',
                            description: `W tym panelu możesz określić swoje preferencje dotyczące przetwarzania Twoich danych osobowych. W każdej chwili możesz je przejrzeć i zmienić, wracając do tego panelu za pomocą linku w stopce strony.`,
                        },
                        {
                            title: 'Niezbędne pliki cookie',
                            description: 'Te pliki cookie są niezbędne do prawidłowego funkcjonowania strony internetowej i nie można ich wyłączyć. Służą one do podstawowych funkcji, takich jak nawigacja i bezpieczeństwo.',
                            linkedCategory: 'necessary'
                        },
                        {
                            title: 'Analityka i wydajność',
                            description: 'Te pliki cookie zbierają informacje o tym, w jaki sposób korzystasz z naszej witryny. Wszystkie dane są anonimizowane i nie mogą być wykorzystane do Twojej identyfikacji. Pomagają nam zrozumieć, które strony są najpopularniejsze, a które wymagają poprawy.',
                            linkedCategory: 'analytics',
                            cookieTable: {
                                caption: 'Lista plików cookie',
                                headers: {
                                    name: 'Nazwa',
                                    domain: 'Domena',
                                    desc: 'Opis'
                                },
                                body: [
                                    {
                                        name: '_ga, _ga_*',
                                        domain: location.hostname,
                                        desc: 'Używane przez Google Analytics do rozróżniania użytkowników.',
                                    },
                                    {
                                        name: '_gid',
                                        domain: location.hostname,
                                        desc: 'Używane przez Google Analytics do rozróżniania użytkowników.',
                                    }
                                ]
                            }
                        },
                        {
                            title: 'Więcej informacji',
                            description: 'W przypadku jakichkolwiek pytań dotyczących polityki w zakresie plików cookie i Twoich wyborów, skontaktuj się poprzez stronę <a href="/kontakt">kontakt</a>.'
                        }
                    ]
                }
            }
        }
    }
});