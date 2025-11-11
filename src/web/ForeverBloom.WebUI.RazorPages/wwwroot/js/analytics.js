// This function sends a page_view event to Google Analytics
export function trackPageView() {
    if (window.googleAnalyticsSettings && window.googleAnalyticsSettings.trackingId && typeof gtag === 'function') {
        gtag('event', 'page_view', {
            page_location: window.location.href,
            page_path: window.location.pathname,
            page_title: document.title
        });
    }
}