// Category page: prevent toggle click from triggering link navigation
(function () {
  function initCategoryTreeToggles() {
    var root = document.getElementById('category-root');
    if (!root) return;
    root.addEventListener('click', function (e) {
      var btn = e.target.closest('.category-toggle');
      if (btn) {
        e.stopPropagation();
      }
    });
  }
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initCategoryTreeToggles);
  } else {
    initCategoryTreeToggles();
  }
})();

// Generic horizontal scrollers (used by Featured row and others)
(function () {
  function initHScroll(wrapper) {
    var scroller = wrapper.querySelector('.h-scroll');
    var btnLeft = wrapper.querySelector('.h-scroll-btn-left');
    var btnRight = wrapper.querySelector('.h-scroll-btn-right');
    if (!scroller || !btnLeft || !btnRight) return;

    function hasOverflow() {
      return (scroller.scrollWidth - scroller.clientWidth) > 1;
    }

    function update() {
      if (hasOverflow()) {
        btnLeft.classList.remove('h-scroll-btn-hidden');
        btnRight.classList.remove('h-scroll-btn-hidden');
        scroller.classList.remove('justify-content-center');
        scroller.classList.add('justify-content-start');
      } else {
        btnLeft.classList.add('h-scroll-btn-hidden');
        btnRight.classList.add('h-scroll-btn-hidden');
        scroller.classList.remove('justify-content-start');
        scroller.classList.add('justify-content-center');
      }
    }

    function scrollByAmount(dir) {
      var first = scroller.firstElementChild;
      var base = first ? first.getBoundingClientRect().width : (scroller.clientWidth * 0.8);
      if (!base || base <= 0) base = 240;
      scroller.scrollBy({ left: dir * (base + 12), behavior: 'smooth' });
    }

    btnLeft.addEventListener('click', function () { scrollByAmount(-1); });
    btnRight.addEventListener('click', function () { scrollByAmount(1); });

    if (document.readyState === 'complete') update();
    else window.addEventListener('load', update);
    if (window.ResizeObserver) {
      var ro = new ResizeObserver(update);
      ro.observe(scroller);
    }
    window.addEventListener('resize', update);
  }

  function initAllHScroll() {
    document.querySelectorAll('.h-scroll-wrapper').forEach(initHScroll);
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initAllHScroll);
  else initAllHScroll();
})();
