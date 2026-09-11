/**
 * Sohba Landing Page Interactive Controller
 * Handles Three.js Ambient Particle Canvas, GSAP Entrance/Scroll Animations,
 * Counter Interpolation, and Lucide Icon Rendering.
 */

(function () {
    'use strict';

    // 1. Accessibility Check: Reduced Motion
    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // 2. Initialize Lucide Icons
    function initIcons() {
        if (typeof lucide !== 'undefined' && typeof lucide.createIcons === 'function') {
            lucide.createIcons();
        }
    }

    // 3. Animated Metric Counters
    function initCounters() {
        const counters = document.querySelectorAll('.counter');
        if (!counters.length) return;

        const observerOptions = { threshold: 0.25 };
        const counterObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const el = entry.target;
                    const targetVal = parseInt(el.getAttribute('data-value'), 10) || 0;
                    const suffix = el.getAttribute('data-suffix') || '';
                    animateCounter(el, targetVal, suffix);
                    observer.unobserve(el);
                }
            });
        }, observerOptions);

        counters.forEach(counter => counterObserver.observe(counter));
    }

    function animateCounter(element, target, suffix) {
        if (prefersReducedMotion || target === 0) {
            element.textContent = target.toLocaleString() + suffix;
            return;
        }

        const duration = 1800; // ms
        const startTime = performance.now();

        function update(currentTime) {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);
            // Ease-out cubic formula
            const easeProgress = 1 - Math.pow(1 - progress, 3);
            const currentVal = Math.floor(easeProgress * target);

            element.textContent = currentVal.toLocaleString() + suffix;

            if (progress < 1) {
                requestAnimationFrame(update);
            } else {
                element.textContent = target.toLocaleString() + suffix;
            }
        }

        requestAnimationFrame(update);
    }

    // 4. Smooth Anchor Scrolling
    function initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                const targetId = this.getAttribute('href');
                if (targetId === '#' || targetId === '') return;

                const targetEl = document.querySelector(targetId);
                if (targetEl) {
                    e.preventDefault();
                    targetEl.scrollIntoView({
                        behavior: prefersReducedMotion ? 'auto' : 'smooth',
                        block: 'start'
                    });
                }
            });
        });
    }

    // 5. GSAP Entrance & Scroll Animations
    function initGSAPAnimations() {
        if (typeof gsap === 'undefined' || prefersReducedMotion) return;

        if (typeof ScrollTrigger !== 'undefined') {
            gsap.registerPlugin(ScrollTrigger);
        }

        // Hero Entrance Timeline
        const heroTl = gsap.timeline({ defaults: { ease: 'power3.out', duration: 0.8 } });

        heroTl.from('.hero-inner .hero-title', { opacity: 0, y: 30, delay: 0.1 })
            .from('.hero-inner .hero-subtitle', { opacity: 0, y: 20 }, '-=0.5')
            .from('.hero-cta-group', { opacity: 0, y: 15 }, '-=0.4')
            .from('.hero-mockup-wrapper', { opacity: 0, y: 40, scale: 0.98 }, '-=0.6');

        // ScrollTrigger Bento Grid Stagger
        if (typeof ScrollTrigger !== 'undefined') {
            const bentoCards = document.querySelectorAll('.bento-card');
            if (bentoCards.length) {
                gsap.from(bentoCards, {
                    scrollTrigger: {
                        trigger: '#showcase',
                        start: 'top 80%',
                        once: true
                    },
                    opacity: 0,
                    y: 35,
                    stagger: 0.12,
                    duration: 0.8,
                    ease: 'power2.out'
                });
            }

            const detailCards = document.querySelectorAll('.feature-detail-card');
            if (detailCards.length) {
                gsap.from(detailCards, {
                    scrollTrigger: {
                        trigger: '#features',
                        start: 'top 80%',
                        once: true
                    },
                    opacity: 0,
                    y: 30,
                    stagger: 0.15,
                    duration: 0.7,
                    ease: 'power2.out'
                });
            }
        }
    }

    // 6. Interactive Three.js Ambient Particle Constellation
    function initThreeScene() {
        if (prefersReducedMotion) return;

        const canvas = document.getElementById('hero-canvas');
        const container = document.getElementById('hero-canvas-container');
        if (!canvas || !container || typeof THREE === 'undefined') return;

        let scene, camera, renderer, particles, particlePositions, linesGeometry, lineMesh;
        let animationFrameId = null;
        let isRunning = false;

        const particleCount = 45;
        const maxDistance = 3.2;
        const particlesData = [];

        function init() {
            const width = container.clientWidth || window.innerWidth;
            const height = container.clientHeight || window.innerHeight;

            scene = new THREE.Scene();
            camera = new THREE.PerspectiveCamera(60, width / height, 0.1, 1000);
            camera.position.z = 10;

            renderer = new THREE.WebGLRenderer({
                canvas: canvas,
                alpha: true,
                antialias: true,
                powerPreference: 'low-power'
            });
            renderer.setSize(width, height);
            renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.5));

            // Particle Geometry & Setup
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
                color: 0x345e69,
                size: 0.16,
                transparent: true,
                opacity: 0.65
            });

            particles = new THREE.Points(pGeometry, pMaterial);
            scene.add(particles);

            // Dynamic Lines Geometry for Connection Network
            linesGeometry = new THREE.BufferGeometry();
            const maxConnections = particleCount * particleCount;
            const linePositions = new Float32Array(maxConnections * 3);
            const lineColors = new Float32Array(maxConnections * 3);

            linesGeometry.setAttribute('position', new THREE.BufferAttribute(linePositions, 3).setUsage(THREE.DynamicDrawUsage));
            linesGeometry.setAttribute('color', new THREE.BufferAttribute(lineColors, 3).setUsage(THREE.DynamicDrawUsage));

            const lineMaterial = new THREE.LineBasicMaterial({
                vertexColors: true,
                transparent: true,
                opacity: 0.25
            });

            lineMesh = new THREE.LineSegments(linesGeometry, lineMaterial);
            scene.add(lineMesh);

            start();
        }

        function animate() {
            if (!isRunning) return;

            animationFrameId = requestAnimationFrame(animate);

            let vertexpos = 0;
            let colorpos = 0;
            let numConnected = 0;

            const positions = particles.geometry.attributes.position.array;
            const linePositions = linesGeometry.attributes.position.array;
            const lineColors = linesGeometry.attributes.color.array;

            for (let i = 0; i < particleCount; i++) {
                const data = particlesData[i];

                positions[i * 3] += data.velocity.x;
                positions[i * 3 + 1] += data.velocity.y;
                positions[i * 3 + 2] += data.velocity.z;

                // Bounce boundaries
                if (positions[i * 3] < -7 || positions[i * 3] > 7) data.velocity.x = -data.velocity.x;
                if (positions[i * 3 + 1] < -5 || positions[i * 3 + 1] > 5) data.velocity.y = -data.velocity.y;
                if (positions[i * 3 + 2] < -3 || positions[i * 3 + 2] > 3) data.velocity.z = -data.velocity.z;

                // Connect nearby particles
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

                        // Sohba brand color rgb(52, 94, 105) = (0.20, 0.37, 0.41)
                        lineColors[colorpos++] = 0.20 * alpha;
                        lineColors[colorpos++] = 0.37 * alpha;
                        lineColors[colorpos++] = 0.41 * alpha;

                        lineColors[colorpos++] = 0.20 * alpha;
                        lineColors[colorpos++] = 0.37 * alpha;
                        lineColors[colorpos++] = 0.41 * alpha;

                        numConnected++;
                    }
                }
            }

            linesGeometry.setDrawRange(0, numConnected * 2);
            linesGeometry.attributes.position.needsUpdate = true;
            linesGeometry.attributes.color.needsUpdate = true;
            particles.geometry.attributes.position.needsUpdate = true;

            particles.rotation.y += 0.0008;
            lineMesh.rotation.y += 0.0008;

            renderer.render(scene, camera);
        }

        function start() {
            if (!isRunning) {
                isRunning = true;
                animate();
            }
        }

        function stop() {
            isRunning = false;
            if (animationFrameId) {
                cancelAnimationFrame(animationFrameId);
                animationFrameId = null;
            }
        }

        function onResize() {
            if (!container || !renderer || !camera) return;
            const width = container.clientWidth || window.innerWidth;
            const height = container.clientHeight || window.innerHeight;

            camera.aspect = width / height;
            camera.updateProjectionMatrix();
            renderer.setSize(width, height);
        }

        // Window resize debouncer
        let resizeTimer = null;
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(onResize, 150);
        }, { passive: true });

        // Pause loop when document is hidden to conserve GPU/battery
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                stop();
            } else {
                start();
            }
        });

        // Pause loop when scrolled past hero
        if ('IntersectionObserver' in window) {
            const heroObserver = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        start();
                    } else {
                        stop();
                    }
                });
            }, { threshold: 0.1 });
            heroObserver.observe(container);
        }

        init();
    }

    // Lifecycle Bootstrap
    function bootstrap() {
        initIcons();
        initCounters();
        initSmoothScroll();
        initGSAPAnimations();
        initThreeScene();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bootstrap);
    } else {
        bootstrap();
    }
})();