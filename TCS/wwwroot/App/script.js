const spamButton = document.getElementById('spam-btn');
const bindsButton = document.getElementById('binds-btn');
const manualButton = document.getElementById('manual-btn');
const underline = document.getElementById('underline');


function updateUnderline(element) {
    const buttonRect = element.getBoundingClientRect();
    const containerRect = document.querySelector('.button-container').getBoundingClientRect();
    const leftOffset = buttonRect.left - containerRect.left;

    underline.style.width = buttonRect.width + 32 + 'px';
    underline.style.left = leftOffset - 16 + 'px';
}

function loadContent(partialViewName) {
    var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");
    $.ajax({
        type: "GET", // Используем GET запрос
        url: "/App/LoadPartialView?partialViewName=" + partialViewName,
        headers: {
            "Authorization": auth_token
        },
        success: function (data) {
            $("#content").html(data);
        },
        error: function (error) {
            // Обработка ошибки
            window.location.href = "/";
        }
    });

}

$(document).ready(function () {
    spamButton.addEventListener('click', function () {
        updateUnderline(spamButton);
        spamButton.classList.add("button-active");
        bindsButton.classList.remove("button-active");
        manualButton.classList.remove("button-active");
        loadContent("Spam");
    });

    bindsButton.addEventListener('click', function () {
        updateUnderline(bindsButton);
        bindsButton.classList.add("button-active");
        spamButton.classList.remove("button-active");
        manualButton.classList.remove("button-active");
        loadContent("Binds");
    });

    manualButton.addEventListener('click', function () {
        updateUnderline(manualButton);
        manualButton.classList.add("button-active");
        bindsButton.classList.remove("button-active");
        spamButton.classList.remove("button-active");
        loadContent("Manual");
    });

    $("#exit-button").on("click", function () {
        $.ajax({
            type: "GET",
            url: "/api/unauthorization",
            success: function (response) {
                // Обработка успешного ответа от сервера
                if (response.status == "ok") {
                    window.location.href = "/";
                }
            },
            error: function (error) {
                // Обработка ошибки
            }
        });

    });

    spamButton.click();
});
