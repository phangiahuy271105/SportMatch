(() => {
    "use strict";

    const venueModal = document.querySelector("#venue-modal");
    const bookingModal = document.querySelector("#booking-modal");
    const detailsHost = document.querySelector("#venue-modal-details");
    const continueButton = document.querySelector("#continue-booking");
    const bookingForm = document.querySelector("#booking-form");
    const errorBox = document.querySelector("#booking-error");
    const slotCount = document.querySelector("#booking-slot-count");
    const matchmakingToggle = document.querySelector("#open-for-matchmaking");
    const matchmakingOptions = document.querySelector("#booking-match-options");
    const bookingDateInput = bookingForm.querySelector("input[name='BookingDate']");
    const visualClasses = ["venue-green", "venue-blue", "venue-orange", "venue-copper", "venue-purple", "venue-teal"];
    let selectedVenue = null;
    let selectedTime = null;

    const formatMoney = value => new Intl.NumberFormat("vi-VN").format(value) + "đ";
    const formatBookingDate = value => {
        const parts = value.split("-");
        return parts.length === 3 ? `${parts[2]}/${parts[1]}/${parts[0]}` : value;
    };
    function updatePrice() {
        if (!selectedVenue) return;
        const count = Number(slotCount.value);
        const hour = Number((selectedTime || "00:00").split(":")[0]);
        const total = Array.from({ length: count }, (_, i) => hour + i >= 16 ? selectedVenue.peakPrice : selectedVenue.price).reduce((sum, value) => sum + value, 0);
        document.querySelector("#booking-price").textContent = formatMoney(total);
        document.querySelector("#booking-deposit").textContent = formatMoney(Math.round(total * 0.3));
        document.querySelector("#booking-summary-duration").textContent = `${count} giờ · ${count} khung liên tiếp`;
    }

    function showModal(modal) {
        modal.classList.remove("is-hidden");
        document.body.style.overflow = "hidden";
    }

    function hideModal(modal) {
        modal.classList.add("is-hidden");
        document.body.style.overflow = "";
    }

    function openVenue(card) {
        selectedVenue = {
            id: card.dataset.venueId,
            name: card.dataset.venueName,
            sport: card.dataset.sportName,
            district: card.dataset.district,
            address: card.dataset.address,
            price: Number(card.dataset.price),
            peakPrice: Number(card.dataset.peakPrice),
            visualClass: card.dataset.visualClass
        };
        selectedTime = null;
        continueButton.disabled = true;
        document.querySelector("#venue-modal-title").textContent = selectedVenue.name;
        document.querySelector("#venue-modal-meta").textContent = `${selectedVenue.sport} · ${selectedVenue.district}`;
        document.querySelector("#venue-modal-address").textContent = `⌖ ${selectedVenue.address}`;
        document.querySelector("#venue-modal-price").textContent = `${formatMoney(selectedVenue.price)}/giờ`;
        const cover = document.querySelector("#venue-modal-cover");
        cover.classList.remove(...visualClasses);
        cover.classList.add(selectedVenue.visualClass);
        detailsHost.replaceChildren(card.querySelector("[data-venue-details]").content.cloneNode(true));
        showModal(venueModal);
    }

    document.querySelectorAll("[data-open-venue]").forEach(button => {
        button.addEventListener("click", () => openVenue(button.closest("[data-venue-card]")));
    });

    const showMoreButton = document.querySelector("#show-more-venues");
    if (showMoreButton) {
        showMoreButton.addEventListener("click", () => {
            document.querySelectorAll(".venue-card-collapsed").forEach(card => card.classList.remove("venue-card-collapsed"));
            showMoreButton.closest(".venue-more-row").remove();
        });
    }

    detailsHost.addEventListener("click", event => {
        const button = event.target.closest("[data-time-slot]");
        if (!button || button.disabled) return;
        detailsHost.querySelectorAll("[data-time-slot]").forEach(item => item.classList.remove("selected"));
        button.classList.add("selected");
        selectedTime = button.dataset.timeSlot;
        continueButton.disabled = false;
    });

    continueButton.addEventListener("click", () => {
        if (!selectedVenue || !selectedTime) return;
        document.querySelector("#booking-venue-id").value = selectedVenue.id;
        document.querySelector("#booking-start-time").value = selectedTime;
        slotCount.value = "1";
        updatePrice();
        document.querySelector("#booking-summary-venue").textContent = selectedVenue.name;
        document.querySelector("#booking-summary-meta").textContent = `${selectedVenue.sport} · ${selectedVenue.district}`;
        document.querySelector("#booking-summary-time").textContent = `${selectedTime} · ${formatBookingDate(bookingDateInput.value)}`;
        hideModal(venueModal);
        showModal(bookingModal);
    });

    slotCount.addEventListener("change", updatePrice);
    matchmakingToggle.addEventListener("change", () => matchmakingOptions.classList.toggle("is-hidden", !matchmakingToggle.checked));

    bookingForm.addEventListener("submit", async event => {
        event.preventDefault();
        errorBox.classList.add("is-hidden");
        const submitButton = bookingForm.querySelector("button[type='submit']");
        submitButton.disabled = true;
        try {
            const response = await fetch(bookingForm.action, { method: "POST", body: new FormData(bookingForm), headers: { "X-Requested-With": "XMLHttpRequest" } });
            const result = await response.json();
            if (!response.ok) throw new Error(result.errors ? Object.values(result.errors).flat().join(" ") : result.message || "Không thể đặt sân.");
            window.location.assign(result.paymentUrl);
        } catch (error) {
            errorBox.textContent = error.message;
            errorBox.classList.remove("is-hidden");
        } finally {
            submitButton.disabled = false;
        }
    });

    document.querySelectorAll("[data-close-modal]").forEach(button => button.addEventListener("click", () => hideModal(button.closest(".modal-backdrop"))));
    document.querySelectorAll(".modal-backdrop").forEach(modal => modal.addEventListener("click", event => { if (event.target === modal) hideModal(modal); }));
    document.addEventListener("keydown", event => { if (event.key === "Escape") document.querySelectorAll(".modal-backdrop:not(.is-hidden)").forEach(hideModal); });
})();
