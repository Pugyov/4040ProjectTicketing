document.addEventListener("DOMContentLoaded", () => {
  const nav = document.querySelector(".sts-site-nav");
  const toggle = nav?.querySelector("[data-sts-nav-toggle]");
  const menu = nav?.querySelector("#mainNav");

  if (!nav || !toggle || !menu) {
    return;
  }

  const desktopMedia = window.matchMedia("(min-width: 992px)");
  const openClass = "sts-site-nav--menu-open";
  const closeTargets = ".sts-site-nav__link, .sts-site-nav__action";

  const render = () => {
    const isOpen = !desktopMedia.matches && nav.classList.contains(openClass);

    nav.classList.toggle(openClass, isOpen);
    toggle.setAttribute("aria-expanded", String(isOpen));

    if (desktopMedia.matches) {
      menu.removeAttribute("aria-hidden");
      return;
    }

    menu.setAttribute("aria-hidden", String(!isOpen));
  };

  const closeMenu = () => {
    nav.classList.remove(openClass);
    render();
  };

  toggle.addEventListener("click", () => {
    if (desktopMedia.matches) {
      return;
    }

    nav.classList.toggle(openClass);
    render();
  });

  nav.addEventListener("click", (event) => {
    if (desktopMedia.matches) {
      return;
    }

    if (event.target instanceof Element && event.target.closest(closeTargets)) {
      closeMenu();
    }
  });

  document.addEventListener("keydown", (event) => {
    if (event.key !== "Escape" || !nav.classList.contains(openClass)) {
      return;
    }

    closeMenu();
    toggle.focus();
  });

  desktopMedia.addEventListener("change", render);

  render();
});
