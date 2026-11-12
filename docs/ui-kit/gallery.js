/* Builds the canvas of gallery.html from catalogue.js, tokens.js and the templates of the entries.
   A row holds boards. A board is a category, or a group of tokens. An item is an entry or a token.
   The address gallery.html#1.3 shows the entry 1.3. The address gallery.html#Grey100 shows the token Grey100. */
(function () {
    'use strict';

    var catalogue = window.CMIX_CATALOGUE || [];
    var tokens = window.CMIX_TOKENS || { groups: [], skipped: [] };
    var nav = document.getElementById('nav');
    var world = document.getElementById('world');
    var filter = document.getElementById('filter');
    var targets = {};
    var items = [];

    function el(tag, className, text) {
        var e = document.createElement(tag);
        if (className) e.className = className;
        if (text !== undefined) e.textContent = text;
        return e;
    }

    function register(key, element, search, zoom) {
        targets[key.toLowerCase()] = { element: element, zoom: zoom };
        if (search) items.push({ key: key, element: element, search: search.toLowerCase(), zoom: zoom });
    }

    function demoOf(entry) { return document.getElementById('entry-' + entry.id); }

    function row(title, subtitle) {
        var r = el('div', 'g-row');
        var l = el('div', 'g-rowlabel', title);
        if (subtitle) l.appendChild(el('small', '', subtitle));
        r.appendChild(l);
        world.appendChild(r);
        return r;
    }

    function board(parent, id, name, source, width) {
        var b = el('section', 'g-board' + (width ? ' ' + width : ''));
        var l = el('div', 'g-boardlabel');
        l.appendChild(el('span', 'g-id', id));
        l.appendChild(el('span', '', name));
        if (source) l.appendChild(el('span', 'g-source', source));
        l.addEventListener('click', function () { go(id); });
        b.appendChild(l);
        parent.appendChild(b);
        register(id, b, null, 1);
        return b;
    }

    function navLink(id, name, count, sub) {
        var a = el('a', sub ? 'g-sub' : '');
        a.href = '#' + id;
        a.appendChild(el('span', 'g-id', id));
        a.appendChild(el('span', '', name));
        if (count) a.appendChild(el('span', 'g-count', count));
        nav.appendChild(a);
    }

    function swatches(group) {
        var grid = el('div', 'g-swatches');
        group.tokens.forEach(function (t) {
            var card = el('div', 'g-swatch');
            card.title = t.note || '';
            var chip = el('div', 'g-chip');
            var fill = el('i');
            fill.style.background = 'var(--' + t.name + ')';
            chip.appendChild(fill);
            var text = el('div', 'g-text');
            text.appendChild(el('div', 'g-tokenname', t.name));
            text.appendChild(el('div', 'g-tokenvalue', t.raw.replace(/\{DynamicResource (\w+)\}/, '$1')));
            card.appendChild(chip);
            card.appendChild(text);
            grid.appendChild(card);
            register(t.name, card, t.name, 2);
        });
        return grid;
    }

    function sample(t) {
        if (t.type === 'CornerRadius') {
            var box = el('span', 'g-radius');
            box.style.borderRadius = 'var(--' + t.name + ')';
            return box;
        }
        if (t.type === 'Double' && /px$/.test(t.css)) {
            var bar = el('span', 'g-bar');
            bar.style.width = 'var(--' + t.name + ')';
            return bar;
        }
        if (t.type === 'FontFamily') {
            var font = el('span', '', 'Ctrl+Shift+E 0123');
            font.style.fontFamily = 'var(--' + t.name + ')';
            return font;
        }
        return document.createTextNode('');
    }

    function table(group) {
        var tbl = el('table', 'g-table');
        group.tokens.forEach(function (t) {
            var tr = el('tr');
            var name = el('td');
            name.appendChild(el('span', 'g-tokenname', t.name));
            var value = el('td');
            value.appendChild(el('span', 'g-tokenvalue', t.raw));
            var visual = el('td');
            visual.appendChild(sample(t));
            tr.appendChild(name);
            tr.appendChild(value);
            tr.appendChild(visual);
            tr.appendChild(el('td', 'g-note', t.note || ''));
            tbl.appendChild(tr);
            register(t.name, tr, t.name, 1.5);
        });
        return tbl;
    }

    /* The states of a demo: the key in data-states, the heading, and the state classes of cmix.css.
       A demo shows the columns that its rows use, in this order. */
    var STATES = [
        ['idle', 'Idle', []],
        ['hover', 'Hover', ['pointerover']],
        ['pressed', 'Pressed', ['pointerover', 'pressed']],
        ['on', 'On', ['checked']],
        ['onhover', 'On + hover', ['checked', 'pointerover']],
        ['selected', 'Selected', ['selected']],
        ['selectedhover', 'Selected + hover', ['selected', 'pointerover']],
        ['highlighted', 'Highlighted', ['highlighted']],
        ['cursor', 'Cursor', ['cursor', 'focus-within']],
        ['cursorselected', 'Cursor + selected', ['cursor', 'selected', 'focus-within']],
        ['cursorunfocused', 'Cursor, unfocused', ['cursor']],
        ['focus', 'Focus', ['focus']],
        ['editing', 'Editing', ['editing']],
        ['recording', 'Recording', ['recording']],
        ['open', 'Open', ['open']],
        ['dimmed', 'Dimmed', ['dimmed']],
        ['candidate', 'Candidate', ['candidate']],
        ['dragsource', 'Drag source', ['dragsource']],
        ['dropblocked', 'Drop blocked', ['dropblocked']],
        ['droptarget', 'Drop target', ['droptarget']],
        ['dropgap', 'Drop gap', ['dropgap']],
        ['droptop', 'Drop top', ['droptop']],
        ['hovered', 'Hovered', ['hovered']],
        ['connecting', 'Connecting', ['connecting']],
        ['connected', 'Connected', ['connected']],
        ['unsynced', 'Not in sync', ['connected', 'unsynced']],
        ['insync', 'In sync', ['insync']],
        ['highlight', 'Modulator hovered', ['highlight']],
        ['disabled', 'Disabled', ['disabled']]
    ];

    /* A child of the template with data-sub is one row: a ControlTheme or a variant, with a fixed letter.
       data-states names the columns that the row has. The default is idle, hover, pressed and disabled.
       data-on adds the two On columns. The state goes on the elements with data-state, or on the first element.
       A child with data-sample is a block under the grid with the same kind of label: an open popup, a group, a layout.
       data-note is a line of text under the label. */
    function stateGrid(entry, content) {
        var holder = document.createDocumentFragment();
        var rows = [], used = {};

        Array.prototype.forEach.call(content.children, function (child) {
            if (!child.hasAttribute('data-sub')) return;
            var wanted = (child.getAttribute('data-states') || 'idle hover pressed disabled').split(/\s+/);
            if (child.hasAttribute('data-on')) wanted = wanted.concat(['on', 'onhover']);
            wanted.forEach(function (w) { used[w] = true; });
            rows.push({ child: child, wanted: wanted });
        });

        var columns = STATES.filter(function (s) { return used[s[0]]; });

        function label(child, letter) {
            var id = entry.id + '.' + letter;
            var name = child.getAttribute('data-name');
            var l = el('div', 'g-sublabel');
            l.appendChild(el('span', 'g-id', id));
            l.appendChild(el('span', 'g-tokenname', name));
            register(id, l, id + ' ' + name, 2);
            return l;
        }

        if (rows.length) {
            var grid = el('div', 'g-states');
            grid.style.gridTemplateColumns = '250px repeat(' + columns.length + ', minmax(124px, max-content))';
            grid.appendChild(el('div'));
            columns.forEach(function (s) { grid.appendChild(el('div', 'g-statehead', s[1])); });
            rows.forEach(function (row) {
                grid.appendChild(label(row.child, row.child.getAttribute('data-sub')));
                columns.forEach(function (s) {
                    var cell = el('div', 'g-cell');
                    if (row.wanted.indexOf(s[0]) >= 0) {
                        Array.prototype.forEach.call(row.child.children, function (c) { cell.appendChild(c.cloneNode(true)); });
                        var marked = cell.querySelectorAll('[data-state]');
                        var states = marked.length ? marked : cell.children;
                        Array.prototype.forEach.call(states, function (t) {
                            s[2].forEach(function (c) { t.classList.add(c); });
                        });
                    }
                    grid.appendChild(cell);
                });
            });
            holder.appendChild(grid);
        }

        Array.prototype.forEach.call(content.children, function (child) {
            if (!child.hasAttribute('data-sample')) return;
            var block = el('div', 'g-sample');
            var head = label(child, child.getAttribute('data-sample'));
            if (child.getAttribute('data-note')) head.appendChild(el('span', 'g-samplenote', child.getAttribute('data-note')));
            block.appendChild(head);
            var body = el('div', 'g-samplebody');
            if (child.getAttribute('style')) body.setAttribute('style', child.getAttribute('style'));
            Array.prototype.forEach.call(child.children, function (c) { body.appendChild(c.cloneNode(true)); });
            block.appendChild(body);
            holder.appendChild(block);
        });

        return holder;
    }

    function entryCard(entry) {
        var card = el('article', 'g-entry');
        var head = el('header');
        head.appendChild(el('span', 'g-id', entry.id));
        head.appendChild(el('span', 'g-name', entry.name));
        head.appendChild(el('span', 'g-source', entry.source));
        card.appendChild(head);
        var demo = demoOf(entry);
        if (demo) {
            var body = el('div', 'g-demo cmix');
            body.setAttribute('data-canvas-live', '');
            body.appendChild(stateGrid(entry, demo.content));
            card.appendChild(body);
        }
        else {
            card.appendChild(entry.removed ? el('div', 'g-removed', 'Removed. ' + entry.removed) : el('div', 'g-todo', 'Not built yet.'));
        }
        register(entry.id, card, entry.id + ' ' + entry.name, 2);
        return card;
    }

    catalogue.forEach(function (part) {
        nav.appendChild(el('div', 'g-part', 'Part ' + part.part + ': ' + part.title));

        part.categories.forEach(function (cat) {
            if (cat.id !== '0') return;
            var r = row(cat.id + ' ' + cat.name, part.title);
            register(cat.id, r, null, 1);
            navLink(cat.id, cat.name, String(tokens.groups.length));
            tokens.groups.forEach(function (group) {
                var colors = group.tokens.every(function (t) { return t.type === 'Color' || t.type === 'SolidColorBrush'; });
                var b = board(r, group.id, group.name, group.source, colors ? 'g-wide' : 'g-xwide');
                b.appendChild(colors ? swatches(group) : table(group));
                if (group.id === '0.2' && tokens.skipped.length) {
                    var names = tokens.skipped.map(function (s) { return s.name; }).join(', ');
                    b.appendChild(el('div', 'g-skipped', 'Not made by the generator, written by hand in cmix.css: ' + names));
                }
                navLink(group.id, group.name, String(group.tokens.length), true);
            });
        });

        var r = row('Part ' + part.part, part.title);
        part.categories.forEach(function (cat) {
            if (cat.id === '0') return;
            var b = board(r, cat.id, cat.name);
            cat.entries.forEach(function (entry) { b.appendChild(entryCard(entry)); });
            navLink(cat.id, cat.name, cat.entries.filter(demoOf).length + '/' + cat.entries.length);
        });
    });

    /* The demo of CategoryPalette shows the six tones of every kind, at the depth 0 and at the depth 1. */
    Array.prototype.forEach.call(world.querySelectorAll('[data-palette]'), function (holder) {
        var roles = ['Fill', 'FillHover', 'FillSelected', 'KindTone', 'KindHover', 'KindSelected'];
        holder.appendChild(el('b'));
        ['Fill', 'FillHover', 'FillSelected', 'Kind', 'KindHover', 'KindSelected'].forEach(function (r) { holder.appendChild(el('b', '', r)); });
        ['Composable', 'ComposableLevel1', 'ComposableLevel2', 'ComposableLevel3', 'Entity', 'Camera', 'Light', 'Texture', 'Filter', 'Modifier', 'Modulator'].forEach(function (kind) {
            [0, 1].forEach(function (depth) {
                holder.appendChild(el('span', '', depth ? '' : kind));
                roles.forEach(function (role) {
                    var chip = el('i');
                    chip.setAttribute('data-kind', kind);
                    chip.style.setProperty('--KindDepth', String(depth));
                    chip.style.background = 'var(--' + role + ')';
                    holder.appendChild(chip);
                });
            });
        });
    });

    /* The demo of AppIcon shows every icon key of icons.js. */
    Array.prototype.forEach.call(world.querySelectorAll('[data-all-icons]'), function (holder) {
        ((window.CMIX_ICONS || {}).icons || []).forEach(function (i) {
            var tile = el('div', 'g-icon');
            tile.title = i.kind + ' (' + i.source + ')';
            var glyph = el('i', 'AppIcon');
            glyph.setAttribute('data-icon-key', i.key);
            tile.appendChild(glyph);
            tile.appendChild(el('span', '', i.key));
            holder.appendChild(tile);
            register(i.key, tile, i.key + ' ' + i.kind, 2);
        });
    });

    window.CMiX.apply(world);

    var canvas = window.CMiXCanvas.attach(document.querySelector('[data-canvas]'));
    var lastHit = null;

    function go(key, animate) {
        var target = targets[String(key).toLowerCase()];
        if (!target) return false;
        if (lastHit) lastHit.classList.remove('g-hit');
        lastHit = null;
        if (target.zoom > 1) { lastHit = target.element; lastHit.classList.add('g-hit'); }
        canvas.flyTo(target.element, target.zoom, animate);
        history.replaceState(null, '', '#' + key);
        return true;
    }

    function matches(query) {
        var q = query.trim().toLowerCase();
        if (!q) return [];
        var exact = items.filter(function (i) { return i.key.toLowerCase() === q; });
        if (exact.length) return exact;
        return items.filter(function (i) { return i.search.indexOf(q) >= 0; });
    }

    filter.addEventListener('input', function () {
        var found = matches(filter.value);
        var active = filter.value.trim() !== '';
        items.forEach(function (i) { i.element.classList.toggle('g-dim', active && found.indexOf(i) < 0); });
    });
    filter.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { filter.value = ''; filter.dispatchEvent(new Event('input')); }
        if (e.key !== 'Enter') return;
        var found = matches(filter.value);
        if (found.length) go(found[0].key);
    });

    nav.addEventListener('click', function (e) {
        var a = e.target.closest('a');
        if (!a) return;
        e.preventDefault();
        go(a.getAttribute('href').slice(1));
    });
    window.addEventListener('hashchange', function () { go(decodeURIComponent(location.hash.slice(1))); });

    var start = decodeURIComponent(location.hash.slice(1));
    if (!(start && go(start, false)) && !canvas.restore()) canvas.fitAll(false);
})();
