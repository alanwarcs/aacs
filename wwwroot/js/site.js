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
        if (window.innerWidth < 768) {
            sidebar.classList.add('d-none'); // Ensure sidebar is visible on larger screens
        }
    }

    // Call handleResize on load and when the window resizes
    handleResize();
    window.addEventListener('resize', handleResize);


    const addBlogButton = document.querySelector(".btn-custom");
    const blogModal = document.getElementById("blog-modal");
    const modalCloseButton = blogModal.querySelector(".modal-header a");
    const modalFade = blogModal.querySelector(".aacs-modal-fade");

    // Function to open the modal
    function openModal() {
        blogModal.classList.add("show");
        document.body.classList.add("modal-open");
    }

    // Function to close the modal
    function closeModal() {
        blogModal.classList.remove("show");
        document.body.classList.remove("modal-open");
    }

    // Event listeners
    addBlogButton.addEventListener("click", function (event) {
        event.preventDefault();
        openModal();
    });

    modalCloseButton.addEventListener("click", function (event) {
        event.preventDefault();
        closeModal();
    });

    modalFade.addEventListener("click", function () {
        closeModal();
    });
});

 // Initialize Quill editor
 const quill = new Quill('#blogContentEditor', {
    theme: 'snow', // Theme can be 'snow' or 'bubble'
    placeholder: 'Enter blog content...',
    modules: {
        toolbar: [
            ['bold', 'italic', 'underline', 'strike'], // Text styles
            ['link', 'image', 'blockquote', 'code-block'], // Links,Images and blocks
            [{ 'header': 1 }, { 'header': 2 }], // Headers
            [{ 'list': 'ordered' }, { 'list': 'bullet' }], // Lists
            [{ 'indent': '-1' }, { 'indent': '+1' }], // Indentation
            [{ 'align': [] }], // Alignment
            [{ 'color': [] }], // Colors
            ['clean'] // Clear formatting
        ]
    }
});

// Get the content as HTML when submitting the form
document.querySelector('form').addEventListener('submit', (event) => {
    event.preventDefault(); // Prevent default form submission
    const blogContentHTML = quill.root.innerHTML; // Get content as HTML
    console.log(blogContentHTML); // Send this HTML to the server
    alert('Blog content saved!');
});
