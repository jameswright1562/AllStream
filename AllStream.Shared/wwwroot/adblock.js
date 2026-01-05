window.allStreamAdSweep = () => {
    const selectors = [
        'iframe[src*="ads"]',
        '[id^="ad"]',
        '[class*="ad"]',
        '[class*="sponsor"]',
        '[class*="banner"]'
    ];

    selectors.forEach(s =>
        document.querySelectorAll(s).forEach(e => e.remove())
    );
};
