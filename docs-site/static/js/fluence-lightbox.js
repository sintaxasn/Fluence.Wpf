(function () {
  function initializeLightbox() {
    const root = document.querySelector(".fluence-home, .fluence-capture-page, .fluence-control-shot-pair");
    if (!root) {
      return;
    }

    const zoomableImages = document.querySelectorAll('img[src*="images/screenshots/"]');
    if (zoomableImages.length === 0) {
      return;
    }

    let dialog = document.getElementById("fluence-image-lightbox");
    if (!dialog) {
      dialog = document.createElement("dialog");
      dialog.id = "fluence-image-lightbox";
      dialog.className = "fluence-lightbox";
      dialog.innerHTML =
        '<button class="fluence-lightbox-close" type="button" aria-label="Close image preview">&times;</button>' +
        '<img class="fluence-lightbox-image" alt="Expanded screenshot preview">';
      document.body.appendChild(dialog);
    }

    const lightboxImage = dialog.querySelector(".fluence-lightbox-image");
    const closeButton = dialog.querySelector(".fluence-lightbox-close");

    zoomableImages.forEach((image) => {
      image.classList.add("fluence-zoomable-image");
      image.addEventListener("click", () => {
        lightboxImage.src = image.currentSrc || image.src;
        lightboxImage.alt = image.alt || "Expanded screenshot preview";
        if (typeof dialog.showModal === "function") {
          dialog.showModal();
        }
      });
    });

    closeButton.addEventListener("click", () => dialog.close());
    dialog.addEventListener("click", (event) => {
      if (event.target === dialog) {
        dialog.close();
      }
    });
    window.addEventListener("keydown", (event) => {
      if (event.key === "Escape" && dialog.open) {
        dialog.close();
      }
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initializeLightbox);
  } else {
    initializeLightbox();
  }
})();
