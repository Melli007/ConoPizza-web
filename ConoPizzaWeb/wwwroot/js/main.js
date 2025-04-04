/*=============== SHOW MENU ===============*/
const navMenu = document.getElementById('nav-menu'),
    navToggle = document.getElementById('nav-toggle'),
    navClose = document.getElementById('nav-close')

/*Menu show */
if (navToggle) {
    navToggle.addEventListener('click', () => {
        navMenu.classList.add('show-menu')
    })
}
/*Menu hidden */
if (navClose) {
    navClose.addEventListener('click', () => {
        navMenu.classList.remove('show-menu')
    })
}
/*=============== REMOVE MENU MOBILE ===============*/
const navLink = document.querySelectorAll('.nav__link')

const linkAction = () => {
    const navMenu = document.getElementById('nav-menu')
    // When we click on each nav__link, we remove the show-menu class
    navMenu.classList.remove('show-menu')
}
navLink.forEach(n => n.addEventListener('click', linkAction))


/*=============== ADD SHADOW HEADER ===============*/
const shadowHeader = () => {
    const header = document.getElementById('header')
    // Add a class if the bottom offset is greater than 50 of the viewport
    window.scrollY >= 50 ? header.classList.add('shadow-header')
        : header.classList.remove('shadow-header')
}
window.addEventListener('scroll', shadowHeader)

/*=============== SWIPER POPULAR ===============*/
const swiperPopular = new Swiper('.popular__swiper', {
    loop: true,
    grabCursor: true,
    slidesPerView: 'auto',
    centeredSlides: 'auto',

});

/*=============== SHOW SCROLL UP ===============*/

const scrollUp = () => {
    const scrollUp = document.getElementById('scroll-up')
    // When the scroll is higher than 350 viewport height, add the show-scroll class to the a tag with the scrollup class
    window.scrollY >= 350 ? scrollUp.classList.add('show-scroll')
        : scrollUp.classList.remove('show-scroll')
}
window.addEventListener('scroll', scrollUp)

/*=============== SCROLL SECTIONS ACTIVE LINK ===============*/

// At the very bottom of main.js
const sections = document.querySelectorAll('section[id]')

const scrollActive = () => {
    const scrollDown = window.scrollY

    sections.forEach(current => {
        const sectionHeight = current.offsetHeight,
            sectionTop = current.offsetTop - 58,
            sectionId = current.getAttribute('id'),
            sectionsClass = document.querySelector('.nav__menu a[href*=' + sectionId + ']')

        // If there's no <a href="#product"> in the nav, sectionsClass is null
        if (sectionsClass) {
            if (scrollDown > sectionTop && scrollDown <= sectionTop + sectionHeight) {
                sectionsClass.classList.add('active-link')
            } else {
                sectionsClass.classList.remove('active-link')
            }
        }
    })
}
window.addEventListener('scroll', scrollActive)

/*=============== SWIPER POPULAR ===============*/


/*=============== SCROLL REVEAL ANIMATION ===============*/
const sr = ScrollReveal({
    origin: 'top',
    distance: '60px',
    duration: 2500,
    delay: 300,
    reset: false
})

// Corrected lines (added quotes around selectors)
sr.reveal('.home__data, .popular__container, .footer')
sr.reveal('.home__board', { delay: 700, distance: '100px', origin: 'right' })
sr.reveal('.home__pizza', { delay: 1400, distance: '100px', origin: 'bottom', rotate: { z: -90 } })
sr.reveal('.home__ingredient', { delay: 2000, interval: 100 })
sr.reveal('.about__data, .recipe__list, .contact__data', { origin: 'right' })
sr.reveal('.about__img, .recipe__img, .contact__image', { origin: 'left' })
sr.reveal('.products__card', { interval: 100 })
sr.reveal('.menu__card', { origin: 'bottom', distance: '200px', duration: 2000, delay: 400, interval: 200 })


/* Apply animations on the Cart page */
if (document.querySelector('.cart-container')) {
    sr.reveal('.cart-container', { origin: 'bottom', distance: '40px', delay: 200 });
    sr.reveal('.cart-item-card', { origin: 'left', distance: '50px', delay: 300, interval: 100 });
    sr.reveal('.cart-total-box', { origin: 'right', distance: '50px', delay: 400 });
    sr.reveal('.alert-info', { origin: 'bottom', distance: '30px', delay: 500 });
}

/* Apply animations on the Checkout page */
if (document.querySelector('.checkout-container')) {
    sr.reveal('.checkout-container', { origin: 'top', distance: '40px', duration: 2000, delay: 200 });
    sr.reveal('.checkout-left', { origin: 'left', distance: '50px', duration: 2000, delay: 300 });
    sr.reveal('.checkout-right', { origin: 'right', distance: '50px', duration: 2000, delay: 400 });
    sr.reveal('.status-timeline', { origin: 'bottom', distance: '40px', duration: 2000, delay: 500 });
    sr.reveal('.status-step', { origin: 'bottom', distance: '30px', duration: 1800, delay: 600, interval: 200 });
    sr.reveal('.order-summary-card', { origin: 'right', distance: '50px', duration: 2000, delay: 500 });
}

/*=============== SCROLL REVEAL FOR FEEDBACK PAGE ===============*/
if (document.querySelector('.feedback-container')) {
    sr.reveal('.feedback-container', {
        origin: 'bottom',
        distance: '40px',
        duration: 2000,
        delay: 200
    });

    // Reveal the form header & sections in sequence
    sr.reveal('.form-header', {
        origin: 'top',
        distance: '40px',
        duration: 2000,
        delay: 300
    });
    sr.reveal('.form-section', {
        origin: 'left',
        distance: '60px',
        duration: 2000,
        delay: 400,
        interval: 200 // so each .form-section animates slightly staggered
    });
}

/*=============== MENU CARD ANIMATIONS ===============*/
function initAnimations() {

    // Mobile-first configuration
    if (window.matchMedia("(max-width: 767.98px)").matches) {
        // Mobile animations
        sr.reveal('.menu-card', {
            interval: 300,
            origin: 'bottom',
            distance: '50px',
            duration: 3200,
            delay: (index) => 300 + (index * 400),
            beforeReveal: (el) => {
                el.style.transform = 'scale(1)';
                el.style.transition = 'transform 0.3s ease';
            }
        });

        // Desktop-only corner animations
        sr.reveal('.sandwiches-corner', {
            origin: 'left',
            distance: '200px',
            duration: 1800,
            delay:1200,
            viewFactor: 0.3
        });

        sr.reveal('.pizza-corner', {
            origin: 'right',
            distance: '200px',
            duration: 1800,
            delay: 2800,
            viewFactor: 0.3
        });

        sr.reveal('.burger-corner', {
            origin: 'left',
            distance: '150px',
            duration: 1800,
            delay: 1800,
            viewFactor: 0.3
        });

        sr.reveal('.Pcontainer', { origin: 'top', distance: '50px', duration: 3200, delay: 3200 })
        sr.reveal('.left', { origin: 'left', distance: '50px', duration: 1200, delay: 300 });
        sr.reveal('.right', { origin: 'right', distance: '50px', duration: 1200, delay: 600 });
    } else {
        // Desktop animations
        sr.reveal('.menu-card', {
            interval: 300,
            origin: 'bottom',
            distance: '50px',
            duration: 3200,
            delay: (index) => 300 + (index * 400),
            beforeReveal: (el) => {
                el.style.transform = 'scale(1)';
                el.style.transition = 'transform 0.3s ease';
            }
        });

        // Desktop-only corner animations
        sr.reveal('.sandwiches-corner', {
            origin: 'left',
            distance: '200px',
            duration: 1800,
            delay: 800,
            viewFactor: 0.3
        });

        sr.reveal('.pizza-corner', {
            origin: 'right',
            distance: '200px',
            duration: 1800,
            delay: 2800,
            viewFactor: 0.3
        });

        sr.reveal('.burger-corner', {
            origin: 'top',
            distance: '150px',
            duration: 1800,
            delay: 1800,
            viewFactor: 0.3
        });
    }
}

// Initial setup
initAnimations();

// Product page specific animations - add existence checks
const productSelectors = ['.Pcontainer', '.left', '.right'];
productSelectors.forEach(selector => {
    if (document.querySelector(selector)) {
        sr.reveal(selector, {
            origin: selector === '.left' ? 'left' :
                selector === '.right' ? 'right' : 'top',
            distance: '350px', // Reduced from 432px to prevent horizontal scrolling
            duration: 1200,
            delay: selector === '.Pcontainer' ? 200 :
                selector === '.left' ? 400 : 600
        });
    }
});

/*=============== PREVENT HORIZONTAL SCROLL BAR ===============*/
document.addEventListener("DOMContentLoaded", () => {
    document.body.style.overflowX = "hidden"; // Ensures no horizontal scroll on page load
});