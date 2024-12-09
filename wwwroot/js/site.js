// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Typing effect for the heading (h6)
function typeText(elementId, text, speed) {
    let i = 0;
    let element = document.getElementById(elementId);
    element.innerHTML = ''; // Clear any existing text
    let interval = setInterval(function () {
        element.innerHTML += text.charAt(i);
        i++;
        if (i === text.length) {
            clearInterval(interval);
        }
    }, speed);
}

// On page load, trigger the typing effects
window.onload = function () {
    typeText("typing-effect-heading", "Welcome to Al Anwar Creativity Studio,", 70); // Adjust typing speed here (in milliseconds)
};

document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.getElementById('admin-sidebar');
    const toggleButton = document.getElementById('admin-sidebar-toggle');

    // Add click event listener to the toggle button
    toggleButton.addEventListener('click', function (e) {
        e.preventDefault();
        sidebar.classList.toggle('d-none');
    });

    // Function to check screen width and adjust sidebar visibility
    function handleResize() {
        if (window.innerWidth > 768) {
            sidebar.classList.remove('d-none'); // Ensure sidebar is visible on larger screens
        }
    }

    // Call handleResize on load and when the window resizes
    handleResize();
    window.addEventListener('resize', handleResize);
});
