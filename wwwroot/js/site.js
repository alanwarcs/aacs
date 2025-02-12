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

    const addButton = document.getElementById("addButton");
    const addModal = document.getElementById("addModal");
    const addModalClose = document.getElementById("addModalClose");
    const modalFade = document.getElementById("modalFade"); // Ensure this exists too!

    if (addButton && addModal && addModalClose && modalFade) {
        // Show/Hide modal logic
        const showAdminModal = () => addModal.classList.add("show");
        const hideAdminModal = () => addModal.classList.remove("show");

        addButton.addEventListener("click", showAdminModal);
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
        document.querySelector('form[asp-action="AddBlog"]').addEventListener('submit', (event) => {
            const blogContentHTML = quill.root.innerHTML; // Get content as HTML
            document.getElementById('blogContent').value = blogContentHTML; // Set the hidden input value
        });
    }

    const editBlogContentEditor = document.querySelector('#editBlogContentEditor');
    if (editBlogContentEditor) {
        const quillEdit = new Quill(editBlogContentEditor, {
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
        document.querySelector('form[asp-action="UpdateBlog"]').addEventListener('submit', (event) => {
            const blogContentHTML = quillEdit.root.innerHTML; // Get content as HTML
            document.getElementById('editBlogContent').value = blogContentHTML; // Set the hidden input value
        });
    }

    const viewButtons = document.querySelectorAll("#viewDetailsButton");
    const viewModal = document.getElementById("viewModal");
    const viewModalClose = document.getElementById("viewModalClose");

    if (viewButtons && viewModal && viewModalClose) {
        viewButtons.forEach(button => {
            button.addEventListener("click", async () => {
                const adminId = button.getAttribute("data-admin-id");

                // Fetch admin details via an API
                try {
                    const response = await fetch(`/Admin/Admin/GetAdminDetails?id=${adminId}`);
                    if (response.ok) {
                        const admin = await response.json();

                        // Populate modal with admin details
                        viewModal.querySelector(".user-status").textContent = admin.status;
                        viewModal.querySelector(".h3").textContent = admin.username;
                        viewModal.querySelector(".view-text.email").textContent = admin.email;
                        viewModal.querySelector(".view-text.phone").textContent = admin.phone;
                        viewModal.querySelector(".view-text.address").textContent = admin.address;

                        // Show the modal
                        viewModal.style.display = "flex";
                    } else {
                        alert("Unable to fetch admin details.");
                    }
                } catch (error) {
                    console.error("Error fetching admin details:", error);
                    alert("An error occurred while fetching admin details.");
                }
            });
        });

        // Close modal
        viewModalClose.addEventListener("click", () => {
            viewModal.style.display = "none";
        });
    }

    const editButtons = document.querySelectorAll("#editDetailsButton");
    const editModal = document.getElementById("editModal");
    const editForm = document.getElementById("editAdminForm");

    if (editButtons && editModal && editForm){
        editButtons.forEach(button => {
            button.addEventListener("click", function () {
                const adminId = this.dataset.adminId; // Fetch admin ID
    
                // Fetch admin details
                fetch(`/Admin/Admin/GetAdminDetails?id=${adminId}`)
                    .then(response => {
                        if (!response.ok) {
                            throw new Error("Failed to fetch admin details.");
                        }
                        return response.json();
                    })
                    .then(data => {
                        if (data) {
                            // Populate form fields with fetched data
                            document.getElementById("editAdminId").value = adminId;
                            document.getElementById("editAdminName").value = data.username;
                            document.getElementById("editAdminEmail").value = data.email;
                            document.getElementById("editAdminPhone").value = data.phone;
                            document.getElementById("editAdminAddress").value = data.address;
                            document.getElementById("editAdminStatus").value = data.status;
    
                            // Show modal
                            editModal.classList.add("show");
                            editModal.style.display = "block";
                        }
                    })
                    .catch(error => {
                        alert("Could not load admin details. Please try again.");
                    });
            });
        });
    
        // Add event listener to close modal
        document.getElementById("editModalClose").addEventListener("click", function (e) {
            e.preventDefault();
            editModal.classList.remove("show");
            editModal.style.display = "none";
        });
    } 

    const editServiceButtons = document.querySelectorAll("#editServiceButton");
    const editServiceModal = document.getElementById("editServiceModal");
    const editServiceForm = document.getElementById("editServiceForm");

    if (editServiceButtons && editServiceModal && editServiceForm){
        editServiceButtons.forEach(button => {
            button.addEventListener("click", function () {
                const serviceId = this.dataset.serviceId;

                fetch(`/Admin/Service/GetServiceDetails?id=${serviceId}`)
                    .then(response => {
                        if (!response.ok) {
                            throw new Error("Failed to fetch service details.");
                        }
                        return response.json();
                    })
                    .then(data => {
                        if (data) {
    
                            document.getElementById("editServiceId").value = serviceId;
                            document.getElementById("editServiceTitle").value = data.title;
                            document.getElementById("editServiceDescription").value = data.description;
                            document.getElementById("editServiceStatus").value = data.status;

                            // Show modal
                            editServiceModal.classList.add("show");
                            editServiceModal.style.display = "block";
                        }
                    })
                    .catch(error => {
                        console.error("Error fetching admin details:", error);
                        alert("Could not load service details. Please try again.");
                    });

            });
        });

        document.getElementById("editServiceModalClose").addEventListener("click", function (e) {
            e.preventDefault();
            editServiceModal.classList.remove("show");
            editServiceModal.style.display = "none";
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