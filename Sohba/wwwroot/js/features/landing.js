/**
 * Sohba Landing v4 — Interactive Controller
 * GSAP entrance + ScrollTrigger reveals + scrubbed depth parallax,
 * CSS-only social-network background (pauses offscreen),
 * scroll progress bar, bento 3D tilt, animated counters, adaptive header,
 * mobile menu, slow-gradient logo letter FX (header + footer).
 * Motion is mandatory by design — no reduced-motion guards.
 */
(function () {
    'use strict';

    var root = document.documentElement;
    /* Low-end devices keep every reveal but lose the idle loops + scrubbed parallax */
    var isLite = (navigator.hardwareConcurrency || 8) <= 4 || (navigator.deviceMemory || 8) <= 4;
    if (isLite) root.classList.add('is-lite');
    var finePointer = window.matchMedia('(hover: hover) and (pointer: fine)').matches;

    /* ---------- 1. Lucide Icons ---------- */
    function initIcons() {
        if (typeof lucide !== 'undefined' && typeof lucide.createIcons === 'function') {
            lucide.createIcons();
        }
    }

    /* ---------- 2. Adaptive Header ---------- */
    function initHeader() {
        const header = document.getElementById('landingHeader');
        if (!header) return;
        const onScroll = () => header.classList.toggle('is-scrolled', window.scrollY > 24);
        window.addEventListener('scroll', onScroll, { passive: true });
        onScroll();
    }

    /* ---------- 3. Mobile Menu ---------- */
    function initMobileMenu() {
        const header = document.getElementById('landingHeader');
        const toggle = document.getElementById('navToggle');
        if (!header || !toggle) return;

        toggle.addEventListener('click', function () {
            const open = header.classList.toggle('nav-open');
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
        });

        header.querySelectorAll('.nav-anchor-link').forEach(function (link) {
            link.addEventListener('click', function () {
                header.classList.remove('nav-open');
                toggle.setAttribute('aria-expanded', 'false');
            });
        });
    }

    /* ---------- 4. Smooth Anchor Scrolling ---------- */
    /* Kept alongside the CSS `scroll-behavior: smooth` so anchor clicks stay
       smooth even on older browsers without CSS support. */
    function initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                const targetId = this.getAttribute('href');
                if (targetId === '#' || targetId === '') return;
                const targetEl = document.querySelector(targetId);
                if (targetEl) {
                    e.preventDefault();
                    targetEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
                }
            });
        });
    }

    /* ---------- 5. Scroll Progress Bar ---------- */
    function initScrollProgress() {
        var bar = document.querySelector('.scroll-progress');
        if (!bar) return;
        var raf = null;

        function update() {
            raf = null;
            var doc = document.documentElement;
            var max = doc.scrollHeight - window.innerHeight;
            var p = max > 0 ? Math.min(window.scrollY / max, 1) : 0;
            bar.style.transform = 'scaleX(' + p + ')';
        }

        window.addEventListener('scroll', function () {
            if (raf === null) raf = requestAnimationFrame(update);
        }, { passive: true });

        window.addEventListener('resize', update, { passive: true });
        update();
    }

    /* ---------- 6. Animated Counters ---------- */
    function initCounters() {
        const counters = document.querySelectorAll('.counter');
        if (!counters.length) return;

        const observer = new IntersectionObserver((entries, obs) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const el = entry.target;
                    animateCounter(el, parseInt(el.getAttribute('data-value'), 10) || 0, el.getAttribute('data-suffix') || '');
                    obs.unobserve(el);
                }
            });
        }, { threshold: 0.4 });

        counters.forEach(c => observer.observe(c));
    }

    function animateCounter(el, target, suffix) {
        const duration = 1600;
        const start = performance.now();
        function tick(now) {
            const progress = Math.min((now - start) / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 3);
            el.textContent = Math.floor(eased * target).toLocaleString() + suffix;
            if (progress < 1) requestAnimationFrame(tick);
            else el.textContent = target.toLocaleString() + suffix;
        }
        requestAnimationFrame(tick);
    }

    /* ---------- 7. GSAP Hero Entrance ---------- */
    function setHeroInitialStates() {
        if (typeof gsap === 'undefined') return;
        gsap.set('.hero-title .line', { yPercent: 110 });
        gsap.set('.hero-pill', { opacity: 0, y: 18 });
        gsap.set('.hero-subtitle', { opacity: 0, y: 22 });
        gsap.set('.hero-cta-group', { opacity: 0, y: 22 });
        gsap.set('.hero-proof', { opacity: 0, y: 16 });
        gsap.set('.hero-visual', { opacity: 0, x: 60, scale: 0.96 });
        gsap.set('.top-badge, .bottom-badge', { opacity: 0, y: 14 });
    }

    function initHeroTimeline() {
        if (typeof gsap === 'undefined') return;
        const tl = gsap.timeline({ defaults: { ease: 'power3.out' } });
        tl.to('.hero-title .line', { yPercent: 0, duration: 1.1, stagger: 0.14 }, 0.1)
            .to('.hero-pill', { opacity: 1, y: 0, duration: 0.8 }, 0.05)
            .to('.hero-subtitle', { opacity: 1, y: 0, duration: 1 }, 0.7)
            .to('.hero-cta-group', { opacity: 1, y: 0, duration: 0.9 }, 0.9)
            .to('.hero-proof', { opacity: 1, y: 0, duration: 0.9 }, 1.05)
            .to('.hero-visual', { opacity: 1, x: 0, scale: 1, duration: 1.3 }, 0.4)
            .to('.top-badge', { opacity: 1, y: 0, duration: 0.9 }, 1.2)
            .to('.bottom-badge', { opacity: 1, y: 0, duration: 0.9 }, 1.35)
            .from('#hero .net-inner', { opacity: 0, duration: 1.6, ease: 'power2.out' }, 0)
            .from('#hero .node', { scale: 0.4, opacity: 0, duration: 0.9, stagger: 0.04, ease: 'back.out(1.8)', clearProps: 'transform,opacity' }, 0.3);
    }

    /* ---------- 8. GSAP ScrollTrigger Reveals ---------- */
    function initScrollReveals() {
        if (typeof gsap === 'undefined' || typeof ScrollTrigger === 'undefined') return;
        gsap.registerPlugin(ScrollTrigger);

        const outsideHero = sel => Array.from(document.querySelectorAll(sel))
            .filter(el => !el.closest('#hero'));

        /* Directional fade reveals share one template: fade-up | fade-right | fade-left */
        const fadePresets = {
            'fade-up': { from: { y: 40 }, start: 'top 85%', duration: 1.1 },
            'fade-right': { from: { x: -50 }, start: 'top 82%', duration: 1.2 },
            'fade-left': { from: { x: 50 }, start: 'top 82%', duration: 1.2 }
        };

        Object.keys(fadePresets).forEach(function (anim) {
            const preset = fadePresets[anim];
            outsideHero('[data-anim="' + anim + '"]').forEach(el => {
                gsap.fromTo(el, Object.assign({ opacity: 0 }, preset.from), {
                    opacity: 1, x: 0, y: 0, duration: preset.duration, ease: 'power3.out',
                    scrollTrigger: { trigger: el, start: preset.start, once: true }
                });
            });
        });

        /* Batch reveals for card grids; same shape, different rhythm per group */
        const batchPresets = [
            { selector: '.bento-card[data-anim="reveal"]', y: 46, scale: 0.97, start: 'top 88%', duration: 1, ease: 'power3.out', stagger: 0.12 },
            { selector: '.stack-chip[data-anim="reveal"]', y: 26, scale: 1, start: 'top 92%', duration: 0.8, ease: 'power2.out', stagger: 0.07 },
            { selector: '.ship-card[data-anim="reveal"]', y: 40, scale: 1, start: 'top 88%', duration: 1, ease: 'power3.out', stagger: 0.14 }
        ];

        batchPresets.forEach(function (preset) {
            gsap.set(preset.selector, { opacity: 0, y: preset.y, scale: preset.scale });
            ScrollTrigger.batch(preset.selector, {
                start: preset.start, once: true,
                onEnter: batch => gsap.to(batch, {
                    opacity: 1, y: 0, scale: 1, duration: preset.duration, ease: preset.ease, stagger: preset.stagger
                })
            });
        });

        outsideHero('.panel-card').forEach(el => {
            gsap.fromTo(el, { opacity: 0, y: 50, rotateX: 6 }, {
                opacity: 1, y: 0, rotateX: 0, duration: 1.2, ease: 'power3.out',
                scrollTrigger: { trigger: el, start: 'top 85%', once: true }
            });
        });

        document.querySelectorAll('.panel-card').forEach(card => {
            gsap.fromTo(card.querySelectorAll('.panel-row'), { opacity: 0, x: 20 }, {
                opacity: 1, x: 0, duration: 0.7, ease: 'power2.out', stagger: 0.1,
                scrollTrigger: { trigger: card, start: 'top 78%', once: true }
            });
        });

        document.querySelectorAll('.deepdive-list').forEach(list => {
            gsap.fromTo(list.querySelectorAll('li'), { opacity: 0, y: 18 }, {
                opacity: 1, y: 0, duration: 0.9, ease: 'power2.out', stagger: 0.13,
                scrollTrigger: { trigger: list, start: 'top 85%', once: true }
            });
        });

        document.querySelectorAll('.topics-col').forEach((col, i) => {
            const items = col.querySelectorAll('.topic-item');
            gsap.fromTo(col, { opacity: 0, y: 40 }, {
                opacity: 1, y: 0, duration: 1.1, ease: 'power3.out', delay: i * 0.15,
                scrollTrigger: { trigger: '.topics-grid', start: 'top 82%', once: true }
            });
            gsap.fromTo(items, { opacity: 0, y: 14 }, {
                opacity: 1, y: 0, duration: 0.7, ease: 'power2.out', stagger: 0.05,
                delay: 0.3 + i * 0.15,
                scrollTrigger: { trigger: '.topics-grid', start: 'top 82%', once: true }
            });
        });

        gsap.fromTo('.metrics-grid .metric-block', { opacity: 0, y: 30 }, {
            opacity: 1, y: 0, duration: 1, ease: 'power3.out', stagger: 0.1,
            scrollTrigger: { trigger: '.metrics-grid', start: 'top 85%', once: true }
        });

        gsap.timeline({ scrollTrigger: { trigger: '.credits-inner', start: 'top 80%', once: true } })
            .fromTo('.credits-emblem', { opacity: 0, scale: 0.7, rotate: -8 }, { opacity: 1, scale: 1, rotate: 0, duration: 0.9, ease: 'back.out(1.6)' })
            .fromTo('.credits-inner .eyebrow', { opacity: 0, y: 16 }, { opacity: 1, y: 0, duration: 0.7 }, '-=0.45')
            .fromTo('.credits-inner h2', { opacity: 0, y: 20 }, { opacity: 1, y: 0, duration: 0.8 }, '-=0.5')
            .fromTo('.credits-name-rule', { opacity: 0, scaleX: 0 }, { opacity: 1, scaleX: 1, duration: 0.7 }, '-=0.5')
            .fromTo('.credits-inner p', { opacity: 0, y: 20 }, { opacity: 1, y: 0, duration: 0.8 }, '-=0.55')
            .fromTo('.credits-links', { opacity: 0, y: 20 }, { opacity: 1, y: 0, duration: 0.8 }, '-=0.55');

        outsideHero('.section-head').forEach(head => {
            gsap.fromTo(head.querySelectorAll('.eyebrow, p'), { opacity: 0, y: 24 }, {
                opacity: 1, y: 0, duration: 0.9, ease: 'power2.out', stagger: 0.1,
                scrollTrigger: { trigger: head, start: 'top 85%', once: true }
            });
        });
    }

    /* ---------- 9. Bento 3D Tilt (mouse-follow) ---------- */
    function initBentoTilt() {
        if (typeof gsap === 'undefined') return;
        if (window.matchMedia('(hover: none)').matches) return;

        document.querySelectorAll('.bento-card').forEach(function (card) {
            gsap.set(card, { transformPerspective: 900 });

            var lift = gsap.quickTo(card, 'y', { duration: 0.35, ease: 'power2.out' });
            var rotX = gsap.quickTo(card, 'rotationX', { duration: 0.45, ease: 'power2.out' });
            var rotY = gsap.quickTo(card, 'rotationY', { duration: 0.45, ease: 'power2.out' });

            card.addEventListener('mouseenter', function () { lift(-5); });

            card.addEventListener('mousemove', function (e) {
                var r = card.getBoundingClientRect();
                var px = (e.clientX - r.left) / r.width - 0.5;
                var py = (e.clientY - r.top) / r.height - 0.5;
                rotY(px * 7);
                rotX(-py * 7);
            });

            card.addEventListener('mouseleave', function () {
                lift(0);
                rotX(0);
                rotY(0);
            });
        });
    }

    /* ---------- 10. Logo Letter FX — slow gradient + magnify (header + footer, letters only) ---------- */
    function initLogoLetterFX() {
        var HOT = [52, 94, 105];
        var WHITE = [255, 255, 255];
        var MAX_DIST = 110;
        var MAX_SCALE = 0.32;
        var LIFT = 5;

        function wrapLetters(el) {
            if (el.dataset.fxWrapped === 'true') return;
            var text = el.textContent;
            el.textContent = '';
            Array.from(text).forEach(function (ch) {
                var span = document.createElement('span');
                span.textContent = ch === ' ' ? '\u00A0' : ch;
                span.style.display = 'inline-block';
                span.style.transition = 'color 0.7s ease-out, transform 0.7s cubic-bezier(0.22, 1, 0.36, 1)';
                el.appendChild(span);
            });
            el.dataset.fxWrapped = 'true';
        }

        function getSpans(container) {
            var fxEl = container.querySelector('.letter-fx');
            return fxEl ? fxEl.querySelectorAll('span') : [];
        }

        function cacheState(container) {
            var spans = getSpans(container);
            var centers = [];
            spans.forEach(function (span) {
                span.style.transform = '';
                var r = span.getBoundingClientRect();
                centers.push(r.left + r.width / 2);
            });
            container._fx = { spans: spans, centers: centers };
        }

        function applyFX(container, clientX) {
            if (!container._fx) return;
            var fx = container._fx;
            fx.spans.forEach(function (span, i) {
                var dist = Math.abs(clientX - fx.centers[i]);
                var t = Math.max(0, 1 - dist / MAX_DIST);
                t = t * t * (3 - 2 * t); // smoothstep

                var k = 1 - t;
                var r = Math.round(HOT[0] + (WHITE[0] - HOT[0]) * k);
                var g = Math.round(HOT[1] + (WHITE[1] - HOT[1]) * k);
                var b = Math.round(HOT[2] + (WHITE[2] - HOT[2]) * k);
                span.style.color = 'rgb(' + r + ',' + g + ',' + b + ')';

                var s = 1 + t * MAX_SCALE;
                span.style.transform = 'translateY(' + (-t * LIFT).toFixed(1) + 'px) scale(' + s.toFixed(3) + ')';
            });
        }

        function resetFX(container) {
            if (!container._fx) return;
            container._fx.spans.forEach(function (span) {
                span.style.color = '';
                span.style.transform = '';
            });
        }

        document.querySelectorAll('.letter-fx').forEach(wrapLetters);

        document.querySelectorAll('.logo-link, .footer-brand').forEach(function (container) {
            var raf = null, x = 0;

            container.addEventListener('mouseenter', function (e) {
                cacheState(container);
                x = e.clientX;
                applyFX(container, x);
            });

            container.addEventListener('mousemove', function (e) {
                x = e.clientX;
                if (raf) return;
                raf = requestAnimationFrame(function () { raf = null; applyFX(container, x); });
            });

            container.addEventListener('mouseleave', function () { resetFX(container); });
        });
    }


    /* ---------- 11. Pause loops offscreen ---------- */
    function initPauseOffscreen() {
        if (!('IntersectionObserver' in window)) return;
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) { e.target.classList.toggle('is-paused', !e.isIntersecting); });
        }, { rootMargin: '120px 0px' });
        document.querySelectorAll('.hero-section, .marquee-section, .has-deco').forEach(function (el) { io.observe(el); });
    }

    /* ---------- 12. Nav spy ---------- */
    function initNavSpy() {
        if (!('IntersectionObserver' in window)) return;
        var links = Array.prototype.slice.call(document.querySelectorAll('.nav-menu .nav-anchor-link'));
        if (!links.length) return;
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (!e.isIntersecting) return;
                links.forEach(function (l) { l.classList.toggle('is-active', l.getAttribute('href') === '#' + e.target.id); });
            });
        }, { rootMargin: '-45% 0px -50% 0px' });
        links.forEach(function (l) {
            var t = document.querySelector(l.getAttribute('href'));
            if (t) io.observe(t);
        });
    }

    /* ---------- 13. Split headings into masked words ---------- */
    function splitWords(el) {
        var text = el.textContent.trim();
        el.setAttribute('aria-label', text);
        el.textContent = '';
        var inner = [];
        text.split(/\s+/).forEach(function (word, i, arr) {
            var w = document.createElement('span');
            w.className = 'w';
            w.setAttribute('aria-hidden', 'true');
            var wi = document.createElement('span');
            wi.className = 'wi';
            wi.textContent = word;
            w.appendChild(wi);
            el.appendChild(w);
            if (i < arr.length - 1) el.appendChild(document.createTextNode(' '));
            inner.push(wi);
        });
        return inner;
    }

    function initHeadingSplit() {
        if (typeof gsap === 'undefined' || typeof ScrollTrigger === 'undefined') return;
        document.querySelectorAll('.section-head h2').forEach(function (h2) {
            if (h2.closest('#hero')) return;
            var words = splitWords(h2);
            gsap.set(words, { yPercent: 115 });
            ScrollTrigger.create({
                trigger: h2, start: 'top 88%', once: true,
                onEnter: function () {
                    gsap.to(words, { yPercent: 0, duration: 0.9, ease: 'power3.out', stagger: 0.06 });
                }
            });
        });
    }

    /* ---------- 14. Scroll-driven depth + storytelling (all scrubbed, transform/opacity only) ---------- */
    function initScrollScenes() {
        if (typeof gsap === 'undefined' || typeof ScrollTrigger === 'undefined') return;
        var mm = gsap.matchMedia();

        /* every size: deep-dive checklist lights up as you read it */
        document.querySelectorAll('.deepdive-list li').forEach(function (li) {
            ScrollTrigger.create({
                trigger: li, start: 'top 62%', end: 'bottom 38%',
                toggleClass: { targets: li, className: 'is-active' }
            });
        });

        /* every size: metrics card "curtain" rises into place */
        gsap.fromTo('.metrics-card', { scale: 0.92 }, {
            scale: 1, ease: 'none',
            scrollTrigger: { trigger: '.metrics-section', start: 'top 95%', end: 'top 35%', scrub: true }
        });

        /* every size: credits orbit spins with the scroll */
        gsap.to('.credits-orbit', {
            rotation: 360, ease: 'none',
            scrollTrigger: { trigger: '.credits-section', start: 'top bottom', end: 'bottom top', scrub: true }
        });

        /* ≥760: connecting line draws through the 3 steps */
        mm.add('(min-width: 760px)', function () {
            gsap.to('.ship-line i', {
                scaleX: 1, ease: 'none',
                scrollTrigger: { trigger: '.ship-grid', start: 'top 75%', end: 'bottom 60%', scrub: true }
            });
        });

        /* ≥900: hero depth, community art parallax, credits network drift */
        mm.add('(min-width: 900px)', function () {
            var heroST = { trigger: '#hero', start: 'top top', end: 'bottom top', scrub: true };
            gsap.to('#hero .net', { yPercent: -9, ease: 'none', scrollTrigger: heroST });
            gsap.to('.hero-mockup-wrapper', { y: -70, ease: 'none', scrollTrigger: heroST });
            gsap.to('.hero-copy', { y: -46, opacity: 0.25, ease: 'none',
                scrollTrigger: { trigger: '#hero', start: '20% top', end: 'bottom top', scrub: true } });

            gsap.fromTo('.credits-section .net', { yPercent: 6 }, { yPercent: -6, ease: 'none',
                scrollTrigger: { trigger: '.credits-section', start: 'top bottom', end: 'bottom top', scrub: true } });

            if (!isLite) {
                document.querySelectorAll('.community-card').forEach(function (card) {
                    card.querySelectorAll('.art-bubble, .discover-chip, .story-ring, .people-stack, .search-orb').forEach(function (el, i) {
                        var d = (i % 2 ? 1 : -1) * 18;
                        gsap.fromTo(el, { y: -d }, { y: d, ease: 'none',
                            scrollTrigger: { trigger: card, start: 'top bottom', end: 'bottom top', scrub: 0.6 } });
                    });
                });
            }

            /* pointer parallax on the hero network */
            var hero = document.getElementById('hero');
            var inner = document.querySelector('#hero .net-inner');
            if (!hero || !inner || !finePointer || isLite) return;
            var qx = gsap.quickTo(inner, 'x', { duration: 0.9, ease: 'power3.out' });
            var qy = gsap.quickTo(inner, 'y', { duration: 0.9, ease: 'power3.out' });
            function onMove(e) {
                var r = hero.getBoundingClientRect();
                qx(((e.clientX - r.left) / r.width - 0.5) * -26);
                qy(((e.clientY - r.top) / r.height - 0.5) * -18);
            }
            hero.addEventListener('mousemove', onMove, { passive: true });
            return function () { hero.removeEventListener('mousemove', onMove); };
        });

        /* ≥1100: side decor — each 3D icon floats at its own depth and pops in */
        mm.add('(min-width: 1100px)', function () {
            document.querySelectorAll('.deco').forEach(function (el) {
                var depth = parseFloat(el.getAttribute('data-depth')) || 0.3;
                var rot = parseFloat(el.getAttribute('data-rot')) || 0;
                gsap.set(el, { rotation: rot });
                if (!isLite) {
                    var host = el.closest('section') || el.parentElement;
                    gsap.fromTo(el, { y: depth * -90 }, { y: depth * 90, rotation: rot + depth * 10, ease: 'none',
                        scrollTrigger: { trigger: host, start: 'top bottom', end: 'bottom top', scrub: 0.7 } });
                }
                gsap.from(el, { opacity: 0, scale: 0.5, duration: 1, ease: 'back.out(1.5)',
                    scrollTrigger: { trigger: el, start: 'top 92%', once: true } });
            });
        });

        /* images/fonts change layout after first paint */
        window.addEventListener('load', function () { ScrollTrigger.refresh(); });
        if (document.fonts && document.fonts.ready) document.fonts.ready.then(function () { ScrollTrigger.refresh(); });
    }

    /* ---------- Bootstrap ---------- */
    function bootstrap() {
        initIcons();
        initHeader();
        initMobileMenu();
        initSmoothScroll();
        initScrollProgress();
        initCounters();
        setHeroInitialStates();
        initHeroTimeline();
        initScrollReveals();
        initHeadingSplit();
        initScrollScenes();
        initBentoTilt();
        initLogoLetterFX();
        initPauseOffscreen();
        initNavSpy();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bootstrap);
    } else {
        bootstrap();
    }
})();
