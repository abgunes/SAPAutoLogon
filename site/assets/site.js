(function () {
  var root = document.documentElement;

  /* the guide rail marks the section being read */
  var links = Array.prototype.slice.call(document.querySelectorAll('.rail-list a'));
  var items = links.map(function (a) {
    return { link: a, target: document.getElementById(a.getAttribute('href').slice(1)) };
  }).filter(function (i) { return i.target; });
  var ticking = false;

  function mark() {
    ticking = false;
    var line = window.innerHeight * 0.3;
    var current = items[0];
    for (var i = 0; i < items.length; i++) {
      if (items[i].target.getBoundingClientRect().top <= line) { current = items[i]; }
    }
    if (window.innerHeight + window.pageYOffset >= document.documentElement.scrollHeight - 2) {
      current = items[items.length - 1];
    }
    items.forEach(function (i) {
      if (i === current) { i.link.setAttribute('aria-current', 'true'); } else { i.link.removeAttribute('aria-current'); }
    });
    var list = current.link.parentNode.parentNode;
    if (window.innerWidth < 960) {
      list.scrollLeft = current.link.offsetLeft - list.clientWidth / 2 + current.link.clientWidth / 2;
    }
  }

  function onScroll() {
    if (!ticking) { ticking = true; window.requestAnimationFrame(mark); }
  }

  if (items.length) {
    window.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('resize', onScroll);
    mark();
  }

  /* light and dark, one button, remembered */
  var button = document.getElementById('mode');

  function isDark() {
    var set = root.getAttribute('data-mode');
    if (set) { return set === 'dark'; }
    return !!(window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
  }

  function label() {
    var dark = isDark();
    var text = dark ? 'Switch to light mode' : 'Switch to dark mode';
    button.setAttribute('aria-label', text);
    button.setAttribute('title', text);
    button.querySelector('.icon-moon').toggleAttribute('hidden', dark);
    button.querySelector('.icon-sun').toggleAttribute('hidden', !dark);
  }

  if (button) {
    button.hidden = false;
    label();
    button.addEventListener('click', function () {
      var next = isDark() ? 'light' : 'dark';
      root.setAttribute('data-mode', next);
      try { localStorage.setItem('sapautologon-mode', next); } catch (e) { /* ignore */ }
      label();
    });
  }

  /* copy buttons on the paths */
  Array.prototype.forEach.call(document.querySelectorAll('.copyline'), function (line) {
    var text = line.querySelector('.measured');
    if (!text || !navigator.clipboard) { return; }
    var copy = document.createElement('button');
    copy.type = 'button';
    copy.className = 'btn btn--small';
    copy.textContent = 'Copy';
    copy.addEventListener('click', function () {
      navigator.clipboard.writeText(text.textContent).then(function () {
        copy.textContent = 'Copied';
        setTimeout(function () { copy.textContent = 'Copy'; }, 1500);
      });
    });
    line.appendChild(copy);
  });

  /* the recording holds still until it is on screen, and stays still for reduced motion */
  Array.prototype.forEach.call(document.querySelectorAll('[data-loop]'), function (box) {
    var img = box.querySelector('img');
    var moving = img.getAttribute('data-src');
    var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduce || !moving) { return; }
    if (!('IntersectionObserver' in window)) { img.setAttribute('src', moving); return; }
    new IntersectionObserver(function (entries, observer) {
      if (entries[0].isIntersecting) { img.setAttribute('src', moving); observer.disconnect(); }
    }).observe(box);
  });
})();
