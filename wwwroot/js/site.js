// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Typing effect for the heading (h6)

document.addEventListener('DOMContentLoaded', function () {
    // text typing effect
    const elementId = "typing-effect-heading";
    const element = document.getElementById(elementId);

    if (element) {
        // Only execute the typing effect if the element exists
        typeText(elementId, "Welcome to Al Anwar Creativity Studio,", 70);
    }


    const sidebar = document.getElementById('admin-sidebar');
    const toggleButton = document.getElementById('admin-sidebar-toggle');

    if (sidebar && toggleButton) {
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
                sidebar.classList.add('d-none'); // Ensure sidebar is visible on smaller screens
            }
        }

        // Call handleResize on load and when the window resizes
        handleResize();
        window.addEventListener('resize', handleResize);
    }

    const addAdminButton = document.getElementById("addAdminButton");
    const addModal = document.getElementById("addModal");
    const addModalClose = document.getElementById("addModalClose");
    const modalFade = document.getElementById("modalFade"); // Ensure this exists too!

    if (addAdminButton && addModal && addModalClose && modalFade) {
        // Show/Hide modal logic
        const showAdminModal = () => addModal.classList.add("show");
        const hideAdminModal = () => addModal.classList.remove("show");

        addAdminButton.addEventListener("click", showAdminModal);
        addModalClose.addEventListener("click", (event) => {
            event.preventDefault();
            hideAdminModal();
        });
        modalFade.addEventListener("click", hideAdminModal);
    }

    const blogContentEditor = document.querySelector('#blogContentEditor');
    if (blogContentEditor) {
        const quill = new Quill(blogContentEditor, {
            theme: 'snow',
            placeholder: 'Enter blog content...',
            modules: {
                toolbar: [
                    ['bold', 'italic', 'underline', 'strike'], // Text styles
                    ['link', 'image', 'blockquote', 'code-block'], // Links, Images, and blocks
                    [{ header: 1 }, { header: 2 }], // Headers
                    [{ list: 'ordered' }, { list: 'bullet' }], // Lists
                    [{ indent: '-1' }, { indent: '+1' }], // Indentation
                    [{ align: [] }], // Alignment
                    [{ color: [] }], // Colors
                    ['clean'], // Clear formatting
                ],
            },
        });

        // Get the content as HTML when submitting the form
        document.querySelector('form').addEventListener('submit', (event) => {
            event.preventDefault(); // Prevent default form submission
            const blogContentHTML = quill.root.innerHTML; // Get content as HTML
            console.log(blogContentHTML); // Send this HTML to the server
            alert('Blog content saved!');
        });
    }
});

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

function toggleCustomDropdown(event) {
    event.preventDefault(); // Prevent default link behavior
    const dropdownMenu = document.getElementById("dropdownUser");

    // Toggle dropdown visibility
    dropdownMenu.style.display = dropdownMenu.style.display === "block" ? "none" : "block";

    // Close dropdown if clicked outside
    document.addEventListener("click", function handleOutsideClick(e) {
        if (!event.target.closest(".custom-dropdown")) {
            dropdownMenu.style.display = "none";
            document.removeEventListener("click", handleOutsideClick); // Remove event listener
        }
    });
}