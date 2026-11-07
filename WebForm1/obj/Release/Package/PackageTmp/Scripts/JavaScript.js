(function () {
    // One orchestrated entrance
    requestAnimationFrame(function () {
        requestAnimationFrame(function () { document.body.classList.remove('is-loading'); });
    });

    var reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Hero blobs drift gently toward the pointer
    var blobs = document.querySelectorAll('.blob');
    if (!reduce && blobs.length > 0) {
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
    if (chips.length > 0 && pieces.length > 0) {
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
    }

    // Visit form submission spinner and validation handler
    document.addEventListener("DOMContentLoaded", function () {
        var btn = document.querySelector('input[id$="btnRequest"]');
        var spinner = document.getElementById("sendSpinner");

        if (btn && spinner) {
            var form = btn.closest("form");
            if (form) {
                form.addEventListener("submit", function () {
                    // Only trigger if ASP.NET client-side validation passes
                    if (typeof Page_ClientValidate === "function" && !Page_ClientValidate()) {
                        return;
                    }
                    btn.disabled = true;
                    spinner.hidden = false;
                });
            }
        }
    });
})();