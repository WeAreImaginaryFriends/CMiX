/* A canvas with pan and zoom, for the gallery and for a mockup.
   Markup: <main data-canvas><div data-world> ... </div></main>. Add data-canvas="auto" to attach on load.
   Ctrl + wheel or pinch: zoom at the pointer. Wheel: scroll. Middle button, Space + drag, or a drag of the background: pan.
   Ctrl + 0: 100%. Shift + 1: show all. + and -: one zoom step.
   An element with data-canvas-live keeps its own pointer input: a drag that starts on it does not pan. */
(function () {
    'use strict';

    var MIN = 0.05, MAX = 16, PAD = 56;
    var STEPS = [0.05, 0.1, 0.25, 0.5, 0.75, 1, 1.5, 2, 3, 4, 6, 8, 12, 16];
    var EASE = 'transform 280ms cubic-bezier(0.215, 0.61, 0.355, 1)';

    function clamp(s) { return Math.min(MAX, Math.max(MIN, s)); }

    function isTyping(target) {
        return !!(target && target.closest && target.closest('input, textarea, select, [contenteditable]'));
    }

    function attach(viewport) {
        var world = viewport.querySelector('[data-world]');
        var storageKey = 'cmix-canvas:' + location.pathname;
        var view = { x: 0, y: 0, s: 1 };
        var spaceDown = false;
        var drag = null;

        var hud = document.createElement('div');
        hud.className = 'cv-hud';
        function hudButton(text, title, action) {
            var b = document.createElement('button');
            b.type = 'button';
            b.textContent = text;
            b.title = title;
            b.addEventListener('click', action);
            hud.appendChild(b);
            return b;
        }
        hudButton('−', 'Zoom out (-)', function () { step(-1); });
        var label = hudButton('100%', 'Zoom to 100% (Ctrl+0)', function () { zoomCenter(1, true); });
        label.className = 'cv-zoom';
        hudButton('+', 'Zoom in (+)', function () { step(1); });
        hudButton('Fit', 'Show all (Shift+1)', function () { fitAll(true); });
        viewport.appendChild(hud);

        function save() {
            try { localStorage.setItem(storageKey, JSON.stringify(view)); } catch (e) { /* no storage */ }
        }

        function apply(animate) {
            world.style.transition = animate ? EASE : 'none';
            world.style.transform = 'translate(' + Math.round(view.x) + 'px,' + Math.round(view.y) + 'px) scale(' + view.s + ')';
            world.style.setProperty('--inv', String(1 / view.s));
            label.textContent = Math.round(view.s * 100) + '%';
            save();
        }

        function size() { return { w: viewport.clientWidth, h: viewport.clientHeight }; }

        function zoomAt(cx, cy, s, animate) {
            s = clamp(s);
            var k = s / view.s;
            view.x = cx - (cx - view.x) * k;
            view.y = cy - (cy - view.y) * k;
            view.s = s;
            apply(animate);
        }

        function zoomCenter(s, animate) { var v = size(); zoomAt(v.w / 2, v.h / 2, s, animate); }

        function step(direction) {
            var next = view.s;
            if (direction > 0) { for (var i = 0; i < STEPS.length; i++) { if (STEPS[i] > view.s * 1.001) { next = STEPS[i]; break; } } }
            else { for (var j = STEPS.length - 1; j >= 0; j--) { if (STEPS[j] < view.s * 0.999) { next = STEPS[j]; break; } } }
            zoomCenter(next, true);
        }

        /* The place of an element in the world, without the transform of the world. */
        function rectOf(element) {
            var x = 0, y = 0, e = element;
            while (e && e !== world) { x += e.offsetLeft; y += e.offsetTop; e = e.offsetParent; }
            return { x: x, y: y, w: element.offsetWidth, h: element.offsetHeight };
        }

        function fitAll(animate) {
            var v = size();
            var w = world.scrollWidth, h = world.scrollHeight;
            if (!w || !h) return;
            view.s = clamp(Math.min((v.w - 2 * PAD) / w, (v.h - 2 * PAD) / h, 1));
            view.x = (v.w - w * view.s) / 2;
            view.y = (v.h - h * view.s) / 2;
            apply(animate);
        }

        /* Shows an element at the largest zoom that shows its whole width, and not more than maxZoom.
           An element that is higher than the view starts at the top. */
        function flyTo(element, maxZoom, animate) {
            var v = size(), r = rectOf(element);
            if (!r.w) return;
            view.s = clamp(Math.min((v.w - 2 * PAD) / r.w, maxZoom || 1));
            view.x = (v.w - r.w * view.s) / 2 - r.x * view.s;
            view.y = r.h * view.s > v.h - 2 * PAD ? PAD - r.y * view.s : (v.h - r.h * view.s) / 2 - r.y * view.s;
            apply(animate !== false);
        }

        viewport.addEventListener('wheel', function (e) {
            e.preventDefault();
            var box = viewport.getBoundingClientRect();
            var dx = e.deltaX, dy = e.deltaY;
            if (e.deltaMode === 1) { dx *= 33; dy *= 33; }
            if (e.ctrlKey || e.metaKey) {
                var rate = Math.abs(dy) < 40 ? 0.01 : 0.0022;
                zoomAt(e.clientX - box.left, e.clientY - box.top, view.s * Math.exp(-dy * rate), false);
            }
            else {
                if (e.shiftKey && !dx) { dx = dy; dy = 0; }
                view.x -= dx;
                view.y -= dy;
                apply(false);
            }
        }, { passive: false });

        viewport.addEventListener('pointerdown', function (e) {
            if (e.target.closest('.cv-hud')) return;
            var live = e.target.closest('[data-canvas-live]') || isTyping(e.target);
            var pan = e.button === 1 || (e.button === 0 && (spaceDown || !live));
            if (!pan) return;
            e.preventDefault();
            drag = { id: e.pointerId, x: e.clientX, y: e.clientY };
            viewport.setPointerCapture(e.pointerId);
            viewport.classList.add('cv-panning');
        });
        viewport.addEventListener('pointermove', function (e) {
            if (!drag || e.pointerId !== drag.id) return;
            view.x += e.clientX - drag.x;
            view.y += e.clientY - drag.y;
            drag.x = e.clientX;
            drag.y = e.clientY;
            apply(false);
        });
        function endDrag(e) {
            if (!drag || e.pointerId !== drag.id) return;
            drag = null;
            viewport.classList.remove('cv-panning');
        }
        viewport.addEventListener('pointerup', endDrag);
        viewport.addEventListener('pointercancel', endDrag);
        viewport.addEventListener('mousedown', function (e) { if (e.button === 1) e.preventDefault(); });

        window.addEventListener('keydown', function (e) {
            if (isTyping(e.target)) return;
            if (e.code === 'Space') { spaceDown = true; viewport.classList.add('cv-space'); e.preventDefault(); }
            else if ((e.ctrlKey || e.metaKey) && (e.key === '0')) { e.preventDefault(); zoomCenter(1, true); }
            else if (e.shiftKey && e.code === 'Digit1') { e.preventDefault(); fitAll(true); }
            else if (!e.ctrlKey && !e.metaKey && (e.key === '+' || e.key === '=')) { step(1); }
            else if (!e.ctrlKey && !e.metaKey && e.key === '-') { step(-1); }
        });
        window.addEventListener('keyup', function (e) {
            if (e.code === 'Space') { spaceDown = false; viewport.classList.remove('cv-space'); }
        });

        function restore() {
            try {
                var saved = JSON.parse(localStorage.getItem(storageKey));
                if (saved && isFinite(saved.x) && isFinite(saved.y) && saved.s > 0) {
                    view = { x: saved.x, y: saved.y, s: clamp(saved.s) };
                    apply(false);
                    return true;
                }
            } catch (e) { /* no storage */ }
            return false;
        }

        return { flyTo: flyTo, fitAll: fitAll, restore: restore, zoom: function () { return view.s; } };
    }

    window.CMiXCanvas = { attach: attach };

    document.addEventListener('DOMContentLoaded', function () {
        Array.prototype.forEach.call(document.querySelectorAll('[data-canvas="auto"]'), function (viewport) {
            var canvas = attach(viewport);
            if (!canvas.restore()) canvas.fitAll(false);
        });
    });
})();
