(() => {
    "use strict";

    const modal = document.querySelector("#match-form-modal");
    const createForm = document.querySelector("#create-match-form");
    const errorBox = document.querySelector("#match-form-error");

    function showModal() { modal.classList.remove("is-hidden"); document.body.style.overflow = "hidden"; }
    function hideModal() { modal.classList.add("is-hidden"); document.body.style.overflow = ""; }
    function showToast(title, message) {
        const toast = document.querySelector("#app-toast");
        document.querySelector("#toast-title").textContent = title;
        document.querySelector("#toast-body").textContent = message;
        toast.classList.add("show");
        window.setTimeout(() => toast.classList.remove("show"), 3600);
    }

    document.querySelector("#open-match-form").addEventListener("click", showModal);
    document.querySelector("[data-close-modal]").addEventListener("click", hideModal);
    modal.addEventListener("click", event => { if (event.target === modal) hideModal(); });
    document.addEventListener("keydown", event => { if (event.key === "Escape") hideModal(); });

    async function submitForm(form) {
        const response = await fetch(form.action, { method: "POST", body: new FormData(form), headers: { "X-Requested-With": "XMLHttpRequest" } });
        const result = await response.json();
        if (!response.ok) throw new Error(result.message || "Yêu cầu không thành công.");
        return result;
    }

    createForm.addEventListener("submit", async event => {
        event.preventDefault();
        errorBox.classList.add("is-hidden");
        const submitButton = createForm.querySelector("button[type='submit']");
        submitButton.disabled = true;
        try {
            const result = await submitForm(createForm);
            hideModal();
            showToast(result.message, "Đang mở trang quản lý kèo.");
            window.setTimeout(() => window.location.assign(result.manageUrl), 700);
        } catch (error) {
            errorBox.textContent = error.message;
            errorBox.classList.remove("is-hidden");
        } finally {
            submitButton.disabled = false;
        }
    });

    document.querySelectorAll(".join-form").forEach(form => {
        form.addEventListener("submit", async event => {
            event.preventDefault();
            const button = form.querySelector("button[type='submit']");
            button.disabled = true;
            try {
                const result = await submitForm(form);
                button.textContent = "✓ Chờ chủ kèo duyệt";
                showToast(result.message, "Sau khi được duyệt, bạn cần cọc 50% phần chia sân để khóa vị trí.");
                window.setTimeout(() => window.location.assign(result.statusUrl), 900);
            } catch (error) {
                button.disabled = false;
                showToast("Chưa thể tham gia", error.message);
            }
        });
    });
})();
