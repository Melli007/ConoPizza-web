    function toggleFilterDropdown() {
            const dropdown = document.getElementById('filterDropdown');
    const element = document.getElementById('myElement');

    dropdown.classList.toggle('show');

    if (dropdown.classList.contains('show')) {
        // If dropdown is visible, remove the marginB class
        element.classList.remove('marginB');
            } else {
        // If dropdown is hidden, add marginB back
        element.classList.add('marginB');
            }
        }

    function resetFilters() {
        document.getElementById('startDate').value = '';
    document.getElementById('endDate').value = '';
        }

    // Close dropdown when clicking outside
    window.onclick = function(event) {
            const dropdown = document.getElementById('filterDropdown');
    const element = document.getElementById('myElement');
    if (!event.target.closest('.filter-wrapper') && dropdown.classList.contains('show')) {
        dropdown.classList.remove('show');
    element.classList.add('marginB'); // Add the marginB class back when closing
                   }
        }

    // Update toggle function to handle new classes
    function toggleDetails(orderId) {
            const details = document.getElementById(`details_${orderId}`);
    const icon = details.previousElementSibling.querySelector('.fa-angle-down');
    details.style.display = details.style.display === 'none' ? 'block' : 'none';
    icon.classList.toggle('active');
        }

    async function updateStatus(orderId, status) {
            try {
                const response = await fetch(`/Admin/UpdateStatus/${orderId}`, {
        method: 'POST',
    headers: {
        'Content-Type': 'application/json',
    'RequestVerificationToken': '@Antiforgery.GetAndStoreTokens(ViewContext.HttpContext).RequestToken'
                    },
    body: JSON.stringify({status})
                });

    const data = await response.json();
    if (data.success) {
                    const details = document.getElementById(`details_${orderId}`);
    // Select the badge in the previous sibling (the order header)
    const badge = details.previousElementSibling.querySelector('.badge');
    badge.className = `badge badge-${status.toLowerCase()}`;
    badge.textContent = status;
                }
            } catch (error) {
        console.error('Error updating status:', error);
            }
        }

    async function cancelOrder(orderId) {
            if (!confirm('Are you sure you want to cancel this order?')) return;
    try {
                const response = await fetch(`/Admin/CancelOrder/${orderId}`, {
        method: 'POST',
    headers: {
        'Content-Type': 'application/json',
    'RequestVerificationToken': '@Antiforgery.GetAndStoreTokens(ViewContext.HttpContext).RequestToken'
                    },
                });
    const data = await response.json();
    if (data.success) {
                    const details = document.getElementById(`details_${orderId}`);
    // Again, select the badge in the order header
    const badge = details.previousElementSibling.querySelector('.badge');
    badge.className = `badge badge-Anuluar`;
    badge.textContent = 'Anuluar';
                }
            } catch (error) {
        console.error('Error canceling order:', error);
            }
        }
