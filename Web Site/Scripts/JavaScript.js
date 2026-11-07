(function () {
  // One orchestrated entrance
  requestAnimationFrame(function () {
    requestAnimationFrame(function () { document.body.classList.remove('is-loading'); });
  });

  var reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;

  // Hero blobs drift gently toward the pointer
  var blobs = document.querySelectorAll('.blob');
  if (!reduce) {
    addEventListener('pointermove', function (e) {
      var x = e.clientX / innerWidth - 0.5, y = e.clientY / innerHeight - 0.5;
      blobs.forEach(function (b) {
        var d = +b.dataset.depth;
        b.style.transform = 'translate(' + x * d + 'px,' + y * d + 'px)';
      });
    }, { passive: true });
  }

  // Collection filter: fade out, swap, fade in
  var chips = document.querySelectorAll('.chip');
  var pieces = Array.prototype.slice.call(document.querySelectorAll('.piece'));
  chips.forEach(function (chip) {
    chip.addEventListener('click', function () {
      chips.forEach(function (c) { c.classList.toggle('active', c === chip); });
      var f = chip.dataset.filter;
      pieces.forEach(function (p) { p.classList.add('is-hidden'); });
      setTimeout(function () {
        pieces.forEach(function (p) { p.hidden = f !== 'all' && p.dataset.cat !== f; });
        requestAnimationFrame(function () {
          pieces.forEach(function (p) { p.classList.remove('is-hidden'); });
        });
      }, reduce ? 0 : 450);
    });
  });
})();