(() => {
    "use strict";
    const page = document.querySelector("[data-payment-page]");
    if (!page) return;
    const currentStatus = page.dataset.paymentStatus;
    const countdown = document.querySelector("#payment-countdown");
    if (countdown) {
        const expiresAt = new Date(countdown.dataset.expires).getTime();
        const tick = () => {
            const seconds = Math.max(0, Math.floor((expiresAt - Date.now()) / 1000));
            countdown.textContent = `${String(Math.floor(seconds / 60)).padStart(2, "0")}:${String(seconds % 60).padStart(2, "0")}`;
            if (seconds === 0) window.location.reload();
        };
        tick(); window.setInterval(tick, 1000);
    }
    if (currentStatus === "Chờ thanh toán") window.setInterval(async () => {
        const response = await fetch(page.dataset.statusUrl, { headers: { "X-Requested-With": "XMLHttpRequest" } });
        if (!response.ok) return;
        const result = await response.json();
        if (result.status !== currentStatus) window.location.reload();
    }, 5000);
})();
