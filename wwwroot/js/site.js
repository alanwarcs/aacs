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

    function initializeQuillEditor(editorSelector, hiddenInputSelector, formSelector) {
        const editor = document.querySelector(editorSelector);
        if (editor) {
            const quill = new Quill(editor, {
                theme: 'snow',
                placeholder: 'Enter blog content...',
                modules: {
                    toolbar: {
                        container: [
                            ['bold', 'italic', 'underline', 'strike'],
                            ['link', 'image', 'blockquote', 'code-block'],
                            [{ header: 1 }, { header: 2 }],
                            [{ list: 'ordered' }, { list: 'bullet' }],
                            [{ indent: '-1' }, { indent: '+1' }],
                            [{ align: [] }],
                            [{ color: [] }],
                            ['clean'],
                        ],
                        handlers: {
                            image: imageHandler
                        }
                    },
                },
            });

            function imageHandler() {
                const input = document.createElement('input');
                input.setAttribute('type', 'file');
                input.setAttribute('accept', 'image/*');
                input.click();

                input.onchange = () => {
                    const file = input.files[0];
                    if (file) {
                        const formData = new FormData();
                        formData.append('image', file);

                        fetch('/api/ImageUpload/upload', {
                            method: 'POST',
                            body: formData,
                        })
                        .then(response => response.json())
                        .then(result => {
                            const range = quill.getSelection();
                            quill.insertEmbed(range.index, 'image', result.imageUrl);
                        })
                        .catch(error => {
                            console.error('Error uploading image:', error);
                        });
                    }
                };
            }

            // Ensure content is updated before form submission
            const form = document.querySelector(formSelector);
            if (form) {
                form.addEventListener('submit', function (event) {
                    document.querySelector(hiddenInputSelector).value = quill.root.innerHTML.trim();
                });
            }

            // Update hidden input field on content change
            quill.on('text-change', function () {
                document.querySelector(hiddenInputSelector).value = quill.root.innerHTML.trim();
            });
        }
    }

    initializeQuillEditor('#blogContentEditor', '#blogContent', 'form[asp-action="AddBlog"]');
    initializeQuillEditor('#editBlogContentEditor', '#editBlogContent', 'form[asp-action="UpdateBlog"]');

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
                            document.getElementById("editId").value = adminId;
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
    
                            document.getElementById("editId").value = serviceId;
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

    const editBlogButtons = document.querySelectorAll("#editBlogButtons");
    const editBlogModal = document.getElementById("editBlogModal");
    const editBlogForm = document.getElementById("editBlogForm");
    
    if (editBlogButtons && editBlogModal && editBlogForm) {
        editBlogButtons.forEach(button => {
            button.addEventListener("click", function () {
                const blogId = this.dataset.blogId;  // Corrected here to match data-blog-id
    
                fetch(`/Admin/Blog/GetBlogDetails?id=${blogId}`)
                    .then(response => {
                        if (!response.ok) {
                            throw new Error("Failed to fetch blog details.");
                        }
                        return response.json();
                    })
                    .then(data => {
                        if (data) {
                            // Populate form with fetched data
                            document.getElementById("editId").value = data.blogId; // Make sure you have the correct field in your form
                            document.getElementById("editBlogTitle").value = data.title;
                            document.getElementById("editBlogAuthor").value = data.author;
                            document.getElementById("editBlogDescription").value = data.description;
                            document.getElementById("editBlogTags").value = data.tags;
                            document.getElementById("editBlogStatus").value = data.status;
    
                            // Set the content of the Quill editor
                            const quillEditor = Quill.find(document.getElementById("editBlogContentEditor"));
                            if (quillEditor) {
                                quillEditor.clipboard.dangerouslyPasteHTML(data.content);
                            } else {
                                console.error("Quill editor not found.");
                            }
    
                            // Show modal
                            editBlogModal.classList.add("show");
                            editBlogModal.style.display = "block";
                        }
                    })
                    .catch(error => {
                        alert("Could not load blog details. Please try again.");
                    });
            });
        });
        document.getElementById("editBlogModalClose").addEventListener("click", function (e) {
            e.preventDefault();
            editBlogModal.classList.remove("show");
            editBlogModal.style.display = "none";
        });
    }   
    
    // Blog Page Specific Code
    if (document.querySelector(".blog-page")) {
        setupCopyToClipboard();
        setupScrollToTop();
    }

    // Define setup functions INSIDE the DOMContentLoaded listener
    function setupCopyToClipboard() {
        const copyButton = document.querySelector("[data-copy-link]");
        if (!copyButton) {
            return;
        }

        copyButton.addEventListener("click", function (event) {
            event.preventDefault();
            navigator.clipboard.writeText(window.location.href)
                .then(() => alert("Link copied to clipboard!"))
                .catch(err => console.error("Failed to copy:", err));
        });
    }

    function setupScrollToTop() {
        const scrollToTopBtn = document.getElementById("scrollToTopBtn");
        if (!scrollToTopBtn) {
            return;
        }

        // Show/hide button on scroll
        window.addEventListener("scroll", () => {
            scrollToTopBtn.style.display = window.scrollY > 300 ? "block" : "none";
        });

        // Smooth scroll to top
        scrollToTopBtn.addEventListener("click", () => {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }
});

async function getBrowserInfo() {
    let browser = "Unknown";

    if (navigator.brave && await navigator.brave.isBrave()) {
        browser = "Brave";
    } else if (navigator.userAgent.includes("Edg/")) {
        browser = "Edge";
    } else if (navigator.userAgent.includes("Vivaldi")) {
        browser = "Vivaldi";
    } else if (navigator.userAgent.includes("OPR") || navigator.userAgent.includes("Opera")) {
        browser = "Opera";
    } else if (navigator.userAgent.includes("SamsungBrowser")) {
        browser = "Samsung Internet";
    } else if (navigator.userAgent.includes("Chrome") && !navigator.userAgent.includes("Edg/")) {
        browser = "Chrome";
    } else if (navigator.userAgent.includes("Safari") && !navigator.userAgent.includes("Chrome")) {
        browser = "Safari";
    } else if (navigator.userAgent.includes("Firefox")) {
        browser = "Firefox";
    }

    let os = "Unknown";
    if (navigator.userAgent.includes("Windows")) os = "Windows";
    else if (navigator.userAgent.includes("Mac OS X")) os = "Mac OS X";
    else if (navigator.userAgent.includes("Linux")) os = "Linux";
    else if (navigator.userAgent.includes("Android")) os = "Android";
    else if (navigator.userAgent.includes("iPhone") || navigator.userAgent.includes("iPad")) os = "iOS";
}

getBrowserInfo();

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

function viewMessage(message, name, email, phone, contactId) {
    document.getElementById('contactName').innerText = name;
    document.getElementById('contactEmail').innerText = email;
    document.getElementById('contactPhone').innerText = phone;
    document.getElementById('contactMessage').innerText = message;
    document.getElementById('viewModal').style.display = 'flex';
    // Mark message as read via AJAX POST using the global URL variable
    $.post(markAsReadUrl, { id: contactId })
      .done(function(response) {
          console.log("Message marked as read:", response);
      })
      .fail(function(jqXHR, textStatus, errorThrown) {
          console.error("Failed marking message as read:", textStatus, errorThrown);
      });
}

document.getElementById('viewModalClose').addEventListener('click', function () {
    document.getElementById('viewModal').style.display = 'none';
});