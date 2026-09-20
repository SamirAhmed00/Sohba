/**
 * Sohba Landing Page — Interactive Controller
 * GSAP entrance + ScrollTrigger reveals, dual Three.js particle scenes
 * (hero + credits), animated metric counters, adaptive header, Lucide icons.
 */

(function () {
    'use strict';

    

    /* ------------------------------------------------------------
       1. Lucide Icons
       ------------------------------------------------------------ */
    function initIcons() {
        if (typeof lucide !== 'undefined' && typeof lucide.createIcons === 'function') {
            lucide.createIcons();
        }
    }

    /* ------------------------------------------------------------
       2. Adaptive Header (solidify on scroll)
       ------------------------------------------------------------ */
    function initHeader() {
        const header = document.getElementById('landingHeader');
        if (!header) return;

        function onScroll() {
            if (window.scrollY > 24) {
                header.classList.add('is-scrolled');
            } else {
                header.classList.remove('is-scrolled');
            }
        }

        window.addEventListener('scroll', onScroll, { passive: true });
        onScroll();
    }

    /* ------------------------------------------------------------
       3. Smooth Anchor Scrolling
       ------------------------------------------------------------ */
    function initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                const targetId = this.getAttribute('href');
                if (targetId === '#' || targetId === '') return;
                const targetEl = document.querySelector(targetId);
                if (targetEl) {
                    e.preventDefault();
                    targetEl.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });
    }

    /* ------------------------------------------------------------
       4. Animated Metric Counters
       ------------------------------------------------------------ */
    function initCounters() {
        const counters = document.querySelectorAll('.counter');
        if (!counters.length) return;

        const observer = new IntersectionObserver((entries, obs) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const el = entry.target;
                    const target = parseInt(el.getAttribute('data-value'), 10) || 0;
                    const suffix = el.getAttribute('data-suffix') || '';
                    animateCounter(el, target, suffix);
                    obs.unobserve(el);
                }
            });
        }, { threshold: 0.4 });

        counters.forEach(c => observer.observe(c));
    }

    function animateCounter(el, target, suffix) {
        if (target === 0) {
            el.textContent = target.toLocaleString() + suffix;
            return;
        }
        const duration = 1600;
        const start = performance.now();

        function tick(now) {
            const elapsed = now - start;
            const progress = Math.min(elapsed / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 3);
            el.textContent = Math.floor(eased * target).toLocaleString() + suffix;
            if (progress < 1) requestAnimationFrame(tick);
            else el.textContent = target.toLocaleString() + suffix;
        }
        requestAnimationFrame(tick);
    }

    /* ------------------------------------------------------------
       5. GSAP: Hero Entrance Timeline
       ------------------------------------------------------------ */
    function initHeroTimeline() {
        if (typeof gsap === 'undefined') return;

        gsap.set('.hero-title .line', { yPercent: 110 });

        const tl = gsap.timeline({ defaults: { ease: 'power3.out' } });

        tl.to('.hero-pill', { opacity: 1, y: 0, duration: 0.9 }, 0.1)
          .to('.hero-title .line', { yPercent: 0, duration: 1.2, stagger: 0.16 }, 0.25)
          .to('.hero-subtitle', { opacity: 1, y: 0, duration: 1.1 }, 0.75)
          .to('.hero-cta-group', { opacity: 1, y: 0, duration: 1, ease: 'power2.out' }, 0.95)
          .to('.hero-proof', { opacity: 1, y: 0, duration: 1 }, 1.1)
          .to('.hero-visual', { opacity: 1, x: 0, scale: 1, duration: 1.4, ease: 'power3.out' }, 0.4)
          .to('.top-badge', { opacity: 1, y: 0, duration: 1 }, 1.2)
          .to('.bottom-badge', { opacity: 1, y: 0, duration: 1 }, 1.35);
    }

    function setHeroInitialStates() {
        if (typeof gsap === 'undefined') return;
        gsap.set('.hero-pill', { opacity: 0, y: 16 });
        gsap.set('.hero-subtitle', { opacity: 0, y: 22 });
        gsap.set('.hero-cta-group', { opacity: 0, y: 22 });
        gsap.set('.hero-proof', { opacity: 0, y: 16 });
        gsap.set('.hero-visual', { opacity: 0, x: 60, scale: 0.96 });
        gsap.set('.top-badge, .bottom-badge', { opacity: 0, y: 14 });
    }

    /* ------------------------------------------------------------
       6. GSAP + ScrollTrigger: Section Reveal System
       ------------------------------------------------------------ */
    function initScrollReveals() {
        if (typeof gsap === 'undefined' || typeof ScrollTrigger === 'undefined' ) return;
        gsap.registerPlugin(ScrollTrigger);

        const outsideHero = sel => Array.from(document.querySelectorAll(sel))
            .filter(el => !el.closest('#hero'));

        // Generic fade-up group (section heads, deepdive copy blocks, list groups)
        const fadeUpEls = outsideHero('[data-anim="fade-up"]');
        fadeUpEls.forEach(el => {
            gsap.fromTo(el, { opacity: 0, y: 40 }, {
                opacity: 1, y: 0, duration: 1.2, ease: 'power3.out',
                scrollTrigger: { trigger: el, start: 'top 85%', once: true }
            });
        });

        // Directional slides for deep-dive alternating rows
        outsideHero('[data-anim="fade-right"]').forEach(el => {
            gsap.fromTo(el, { opacity: 0, x: -50 }, {
                opacity: 1, x: 0, duration: 1.3, ease: 'power3.out',
                scrollTrigger: { trigger: el, start: 'top 82%', once: true }
            });
        });

        outsideHero('[data-anim="fade-left"]').forEach(el => {
            gsap.fromTo(el, { opacity: 0, x: 50 }, {
                opacity: 1, x: 0, duration: 1.3, ease: 'power3.out',
                scrollTrigger: { trigger: el, start: 'top 82%', once: true }
            });
        });

        // Bento / stack chip batch reveal — staggered, slow, orchestrated
        ScrollTrigger.batch('.bento-card[data-anim="reveal"]', {
            start: 'top 88%',
            once: true,
            onEnter: batch => gsap.to(batch, {
                opacity: 1, y: 0, scale: 1, duration: 1.1, ease: 'power3.out', stagger: 0.14
            })
        });
        gsap.set('.bento-card[data-anim="reveal"]', { opacity: 0, y: 46, scale: 0.97 });

        ScrollTrigger.batch('.stack-chip[data-anim="reveal"]', {
            start: 'top 92%',
            once: true,
            onEnter: batch => gsap.to(batch, {
                opacity: 1, y: 0, duration: 0.9, ease: 'power2.out', stagger: 0.08
            })
        });
        gsap.set('.stack-chip[data-anim="reveal"]', { opacity: 0, y: 26 });

        // Deep-dive visual panel cards — slight rotation-in for a premium feel
        outsideHero('.panel-card').forEach(el => {
            gsap.fromTo(el, { opacity: 0, y: 50, rotateX: 6 }, {
                opacity: 1, y: 0, rotateX: 0, duration: 1.3, ease: 'power3.out',
                scrollTrigger: { trigger: el, start: 'top 85%', once: true }
            });
        });

        // Panel rows inside each deep-dive visual — staggered line-by-line
        document.querySelectorAll('.panel-card').forEach(card => {
            const rows = card.querySelectorAll('.panel-row');
            gsap.fromTo(rows, { opacity: 0, x: 20 }, {
                opacity: 1, x: 0, duration: 0.8, ease: 'power2.out', stagger: 0.12,
                scrollTrigger: { trigger: card, start: 'top 78%', once: true }
            });
        });

        // Deep-dive list items — slow staggered reveal
        document.querySelectorAll('.deepdive-list').forEach(list => {
            const items = list.querySelectorAll('li');
            gsap.fromTo(items, { opacity: 0, y: 18 }, {
                opacity: 1, y: 0, duration: 1, ease: 'power2.out', stagger: 0.14,
                scrollTrigger: { trigger: list, start: 'top 85%', once: true }
            });
        });

        // Topics columns — reveal each column then stagger its items
        document.querySelectorAll('.topics-col').forEach((col, i) => {
            const items = col.querySelectorAll('.topic-item');
            gsap.fromTo(col, { opacity: 0, y: 40 }, {
                opacity: 1, y: 0, duration: 1.2, ease: 'power3.out', delay: i * 0.15,
                scrollTrigger: { trigger: '.topics-grid', start: 'top 82%', once: true }
            });
            gsap.fromTo(items, { opacity: 0, y: 14 }, {
                opacity: 1, y: 0, duration: 0.8, ease: 'power2.out', stagger: 0.06,
                delay: 0.3 + i * 0.15,
                scrollTrigger: { trigger: '.topics-grid', start: 'top 82%', once: true }
            });
        });

        // Metrics — subtle rise for the whole grid
        gsap.fromTo('.metrics-grid .metric-block', { opacity: 0, y: 30 }, {
            opacity: 1, y: 0, duration: 1.1, ease: 'power3.out', stagger: 0.12,
            scrollTrigger: { trigger: '.metrics-grid', start: 'top 85%', once: true }
        });

        // Credits — orchestrated single reveal moment
        gsap.timeline({
            scrollTrigger: { trigger: '.credits-inner', start: 'top 80%', once: true }
        })
        .fromTo('.credits-emblem', { opacity: 0, scale: 0.7, rotate: -8 }, { opacity: 1, scale: 1, rotate: 0, duration: 1, ease: 'back.out(1.6)' })
        .fromTo('.credits-inner .eyebrow', { opacity: 0, y: 16 }, { opacity: 1, y: 0, duration: 0.8 }, '-=0.5')
        .fromTo('.credits-inner h2', { opacity: 0, y: 20 }, { opacity: 1, y: 0, duration: 0.9 }, '-=0.55')
        .fromTo('.credits-inner p', { opacity: 0, y: 20 }, { opacity: 1, y: 0, duration: 0.9 }, '-=0.6')
        .fromTo('.credits-links', { opacity: 0, y: 20 }, { opacity: 1, y: 0, duration: 0.9 }, '-=0.6');

        // Section eyebrow/heading micro-reveal (mask-style rise) for section-heads not already covered
        outsideHero('.section-head').forEach(head => {
            const kids = head.querySelectorAll('.eyebrow, h2, p');
            gsap.fromTo(kids, { opacity: 0, y: 24 }, {
                opacity: 1, y: 0, duration: 1, ease: 'power2.out', stagger: 0.12,
                scrollTrigger: { trigger: head, start: 'top 85%', once: true }
            });
        });
    }

    /* ------------------------------------------------------------
       7. Three.js — Reusable Particle Constellation
       ------------------------------------------------------------ */
    function createParticleScene(opts) {
        const canvas = document.getElementById(opts.canvasId);
        const container = document.getElementById(opts.containerId);
        if ( !canvas || !container || typeof THREE === 'undefined') return;

        let scene, camera, renderer, particles, particlePositions, linesGeometry, lineMesh;
        let animationFrameId = null;
        let isRunning = false;

        const particleCount = opts.particleCount || 45;
        const maxDistance = opts.maxDistance || 3.2;
        const color = opts.color || [0.20, 0.37, 0.41];
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
                const x = (Math.random() - 0.5) * 14;
                const y = (Math.random() - 0.5) * 10;
                const z = (Math.random() - 0.5) * 6;
                particlePositions[i * 3] = x;
                particlePositions[i * 3 + 1] = y;
                particlePositions[i * 3 + 2] = z;
                particlesData.push({
                    velocity: new THREE.Vector3(
                        (Math.random() - 0.5) * 0.008,
                        (Math.random() - 0.5) * 0.008,
                        (Math.random() - 0.5) * 0.004
                    )
                });
            }

            pGeometry.setAttribute('position', new THREE.BufferAttribute(particlePositions, 3));

            const pMaterial = new THREE.PointsMaterial({
                color: new THREE.Color(color[0], color[1], color[2]),
                size: 0.16,
                transparent: true,
                opacity: 0.65
            });

            particles = new THREE.Points(pGeometry, pMaterial);
            scene.add(particles);

            linesGeometry = new THREE.BufferGeometry();
            const maxConnections = particleCount * particleCount;
            const linePositions = new Float32Array(maxConnections * 3);
            const lineColors = new Float32Array(maxConnections * 3);

            linesGeometry.setAttribute('position', new THREE.BufferAttribute(linePositions, 3).setUsage(THREE.DynamicDrawUsage));
            linesGeometry.setAttribute('color', new THREE.BufferAttribute(lineColors, 3).setUsage(THREE.DynamicDrawUsage));

            const lineMaterial = new THREE.LineBasicMaterial({ vertexColors: true, transparent: true, opacity: 0.25 });
            lineMesh = new THREE.LineSegments(linesGeometry, lineMaterial);
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

                if (positions[i * 3] < -7 || positions[i * 3] > 7) data.velocity.x = -data.velocity.x;
                if (positions[i * 3 + 1] < -5 || positions[i * 3 + 1] > 5) data.velocity.y = -data.velocity.y;
                if (positions[i * 3 + 2] < -3 || positions[i * 3 + 2] > 3) data.velocity.z = -data.velocity.z;

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

        function onResize() {
            if (!container || !renderer || !camera) return;
            const width = container.clientWidth || window.innerWidth;
            const height = container.clientHeight || window.innerHeight;
            camera.aspect = width / height;
            camera.updateProjectionMatrix();
            renderer.setSize(width, height);
        }

        let resizeTimer = null;
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(onResize, 150);
        }, { passive: true });

        document.addEventListener('visibilitychange', () => {
            document.hidden ? stop() : start();
        });

        if ('IntersectionObserver' in window) {
            const io = new IntersectionObserver(entries => {
                entries.forEach(entry => entry.isIntersecting ? start() : stop());
            }, { threshold: 0.08 });
            io.observe(container);
        }

        init();
    }

    /* ------------------------------------------------------------
       Bootstrap
       ------------------------------------------------------------ */
    function bootstrap() {
        initIcons();
        initHeader();
        initSmoothScroll();
        initCounters();
        setHeroInitialStates();
        initHeroTimeline();
        initScrollReveals();

        createParticleScene({
            canvasId: 'hero-canvas',
            containerId: 'hero-canvas-container',
            particleCount: 45,
            maxDistance: 3.2,
            color: [0.20, 0.37, 0.41] // brand teal
        });

        createParticleScene({
            canvasId: 'credits-canvas',
            containerId: 'credits-canvas-container',
            particleCount: 30,
            maxDistance: 3.6,
            color: [0.72, 0.33, 0.37] // rose clay accent
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bootstrap);
    } else {
        bootstrap();
    }
})();