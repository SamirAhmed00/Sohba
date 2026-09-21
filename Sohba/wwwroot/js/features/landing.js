/**
 * Sohba Landing v3 — Interactive Controller
 * GSAP entrance + ScrollTrigger reveals, full-screen Three.js particle scenes,
 * scroll progress bar, bento 3D tilt, animated counters, adaptive header,
 * mobile menu, slow-gradient logo letter FX (header + footer).
 * Motion is mandatory by design — no reduced-motion guards.
 */
(function () {
    'use strict';

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

    /* ---------- 4.5 Scroll Progress Bar ---------- */
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

    /* ---------- 5. Animated Counters ---------- */
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

    /* ---------- 6. GSAP Hero Entrance ---------- */
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
            .to('.bottom-badge', { opacity: 1, y: 0, duration: 0.9 }, 1.35);
    }

    /* ---------- 7. GSAP ScrollTrigger Reveals ---------- */
    function initScrollReveals() {
        if (typeof gsap === 'undefined' || typeof ScrollTrigger === 'undefined') return;
        gsap.registerPlugin(ScrollTrigger);

        const outsideHero = sel => Array.from(document.querySelectorAll(sel))
            .filter(el => !el.closest('#hero'));

        outsideHero('[data-anim="fade-up"]').forEach(el => {
            gsap.fromTo(el, { opacity: 0, y: 40 }, {
                opacity: 1, y: 0, duration: 1.1, ease: 'power3.out',
                scrollTrigger: { trigger: el, start: 'top 85%', once: true }
            });
        });

        outsideHero('[data-anim="fade-right"]').forEach(el => {
            gsap.fromTo(el, { opacity: 0, x: -50 }, {
                opacity: 1, x: 0, duration: 1.2, ease: 'power3.out',
                scrollTrigger: { trigger: el, start: 'top 82%', once: true }
            });
        });

        outsideHero('[data-anim="fade-left"]').forEach(el => {
            gsap.fromTo(el, { opacity: 0, x: 50 }, {
                opacity: 1, x: 0, duration: 1.2, ease: 'power3.out',
                scrollTrigger: { trigger: el, start: 'top 82%', once: true }
            });
        });

        gsap.set('.bento-card[data-anim="reveal"]', { opacity: 0, y: 46, scale: 0.97 });
        ScrollTrigger.batch('.bento-card[data-anim="reveal"]', {
            start: 'top 88%', once: true,
            onEnter: batch => gsap.to(batch, { opacity: 1, y: 0, scale: 1, duration: 1, ease: 'power3.out', stagger: 0.12 })
        });

        gsap.set('.stack-chip[data-anim="reveal"]', { opacity: 0, y: 26 });
        ScrollTrigger.batch('.stack-chip[data-anim="reveal"]', {
            start: 'top 92%', once: true,
            onEnter: batch => gsap.to(batch, { opacity: 1, y: 0, duration: 0.8, ease: 'power2.out', stagger: 0.07 })
        });

        gsap.set('.ship-card[data-anim="reveal"]', { opacity: 0, y: 40 });
        ScrollTrigger.batch('.ship-card[data-anim="reveal"]', {
            start: 'top 88%', once: true,
            onEnter: batch => gsap.to(batch, { opacity: 1, y: 0, duration: 1, ease: 'power3.out', stagger: 0.14 })
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
            gsap.fromTo(head.querySelectorAll('.eyebrow, h2, p'), { opacity: 0, y: 24 }, {
                opacity: 1, y: 0, duration: 0.9, ease: 'power2.out', stagger: 0.1,
                scrollTrigger: { trigger: head, start: 'top 85%', once: true }
            });
        });
    }

    /* ---------- 7.5 Bento 3D Tilt (mouse-follow) ---------- */
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

    /* ---------- 8. Three.js Particle Constellation (full-area) ---------- */
    function createParticleScene(opts) {
        const canvas = document.getElementById(opts.canvasId);
        const container = document.getElementById(opts.containerId);
        if (!canvas || !container || typeof THREE === 'undefined') return;

        let scene, camera, renderer, particles, particlePositions, linesGeometry, lineMesh;
        let animationFrameId = null;
        let isRunning = false;

        const particleCount = opts.particleCount || 45;
        const maxDistance = opts.maxDistance || 3.2;
        const color = opts.color;
        const bounds = opts.bounds || { x: 7, y: 5, z: 3 };
        const particlesData = [];

        function init() {
            const width = container.clientWidth || window.innerWidth;
            const height = container.clientHeight || window.innerHeight;

            scene = new THREE.Scene();
            camera = new THREE.PerspectiveCamera(60, width / height, 0.1, 1000);
            camera.position.z = 10;

            renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true, powerPreference: 'low-power' });
            renderer.setSize(width, height);
            renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.5));

            const pGeometry = new THREE.BufferGeometry();
            particlePositions = new Float32Array(particleCount * 3);

            for (let i = 0; i < particleCount; i++) {
                particlePositions[i * 3] = (Math.random() - 0.5) * bounds.x * 2;
                particlePositions[i * 3 + 1] = (Math.random() - 0.5) * bounds.y * 2;
                particlePositions[i * 3 + 2] = (Math.random() - 0.5) * bounds.z * 2;
                particlesData.push({
                    velocity: new THREE.Vector3(
                        (Math.random() - 0.5) * 0.008,
                        (Math.random() - 0.5) * 0.008,
                        (Math.random() - 0.5) * 0.004
                    )
                });
            }
            pGeometry.setAttribute('position', new THREE.BufferAttribute(particlePositions, 3));

            particles = new THREE.Points(pGeometry, new THREE.PointsMaterial({
                color: new THREE.Color(color[0], color[1], color[2]),
                size: 0.15, transparent: true, opacity: 0.7
            }));
            scene.add(particles);

            linesGeometry = new THREE.BufferGeometry();
            const maxConnections = particleCount * particleCount;
            linesGeometry.setAttribute('position', new THREE.BufferAttribute(new Float32Array(maxConnections * 3), 3).setUsage(THREE.DynamicDrawUsage));
            linesGeometry.setAttribute('color', new THREE.BufferAttribute(new Float32Array(maxConnections * 3), 3).setUsage(THREE.DynamicDrawUsage));

            lineMesh = new THREE.LineSegments(linesGeometry, new THREE.LineBasicMaterial({ vertexColors: true, transparent: true, opacity: 0.3 }));
            scene.add(lineMesh);

            start();
        }

        function animate() {
            if (!isRunning) return;
            animationFrameId = requestAnimationFrame(animate);

            let vertexpos = 0, colorpos = 0, numConnected = 0;
            const positions = particles.geometry.attributes.position.array;
            const linePositions = linesGeometry.attributes.position.array;
            const lineColors = linesGeometry.attributes.color.array;

            for (let i = 0; i < particleCount; i++) {
                const data = particlesData[i];
                positions[i * 3] += data.velocity.x;
                positions[i * 3 + 1] += data.velocity.y;
                positions[i * 3 + 2] += data.velocity.z;

                if (positions[i * 3] < -bounds.x || positions[i * 3] > bounds.x) data.velocity.x = -data.velocity.x;
                if (positions[i * 3 + 1] < -bounds.y || positions[i * 3 + 1] > bounds.y) data.velocity.y = -data.velocity.y;
                if (positions[i * 3 + 2] < -bounds.z || positions[i * 3 + 2] > bounds.z) data.velocity.z = -data.velocity.z;

                for (let j = i + 1; j < particleCount; j++) {
                    const dx = positions[i * 3] - positions[j * 3];
                    const dy = positions[i * 3 + 1] - positions[j * 3 + 1];
                    const dz = positions[i * 3 + 2] - positions[j * 3 + 2];
                    const dist = Math.sqrt(dx * dx + dy * dy + dz * dz);

                    if (dist < maxDistance) {
                        const alpha = 1.0 - dist / maxDistance;
                        linePositions[vertexpos++] = positions[i * 3];
                        linePositions[vertexpos++] = positions[i * 3 + 1];
                        linePositions[vertexpos++] = positions[i * 3 + 2];
                        linePositions[vertexpos++] = positions[j * 3];
                        linePositions[vertexpos++] = positions[j * 3 + 1];
                        linePositions[vertexpos++] = positions[j * 3 + 2];
                        lineColors[colorpos++] = color[0] * alpha;
                        lineColors[colorpos++] = color[1] * alpha;
                        lineColors[colorpos++] = color[2] * alpha;
                        lineColors[colorpos++] = color[0] * alpha;
                        lineColors[colorpos++] = color[1] * alpha;
                        lineColors[colorpos++] = color[2] * alpha;
                        numConnected++;
                    }
                }
            }

            linesGeometry.setDrawRange(0, numConnected * 2);
            linesGeometry.attributes.position.needsUpdate = true;
            linesGeometry.attributes.color.needsUpdate = true;
            particles.geometry.attributes.position.needsUpdate = true;

            particles.rotation.y += 0.0007;
            lineMesh.rotation.y += 0.0007;

            renderer.render(scene, camera);
        }

        function start() { if (!isRunning) { isRunning = true; animate(); } }
        function stop() { isRunning = false; if (animationFrameId) { cancelAnimationFrame(animationFrameId); animationFrameId = null; } }

        let resizeTimer = null;
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(() => {
                if (!container || !renderer || !camera) return;
                const width = container.clientWidth || window.innerWidth;
                const height = container.clientHeight || window.innerHeight;
                camera.aspect = width / height;
                camera.updateProjectionMatrix();
                renderer.setSize(width, height);
            }, 150);
        }, { passive: true });

        document.addEventListener('visibilitychange', () => document.hidden ? stop() : start());

        if ('IntersectionObserver' in window) {
            const io = new IntersectionObserver(entries => {
                entries.forEach(entry => entry.isIntersecting ? start() : stop());
            }, { threshold: 0.08 });
            io.observe(container);
        }

        init();
    }

    /* ---------- 9. Logo Letter FX — slow gradient + magnify (header + footer, letters only) ---------- */
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
        initBentoTilt();
        initLogoLetterFX();

        createParticleScene({
            canvasId: 'hero-canvas',
            containerId: 'hero-canvas-container',
            particleCount: 80,              
            maxDistance: 4.0,               
            color: [52 / 255, 94 / 255, 105 / 255],   
            bounds: { x: 14, y: 9, z: 4 }    
        });

        createParticleScene({
            canvasId: 'credits-canvas',
            containerId: 'credits-canvas-container',
            particleCount: 40,
            maxDistance: 3.6,
            color: [0.85, 0.96, 0.91]      
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bootstrap);
    } else {
        bootstrap();
    }
})();