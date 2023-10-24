const spamButton = document.getElementById('spam-btn');
const bindsButton = document.getElementById('binds-btn');
const manualButton = document.getElementById('manual-btn');
const underline = document.getElementById('underline');
const notificationContainer = document.getElementById('notification-container');

function showNotification(notificationText) {
    const notification = document.createElement('div');
    notification.className = 'notification';
    const notificationContent = document.createElement('div');
    notificationContent.className = 'notification-content';
    const image = document.createElement('img');
    image.src = '/Authorization/error.svg';
    image.alt = 'Ошибка';
    // Создаем элемент для текста
    const text = document.createElement('div');
    text.textContent = notificationText;
    notificationContent.appendChild(image);
    notificationContent.appendChild(text);

    notification.appendChild(notificationContent);
    const existingNotifications = notificationContainer.querySelectorAll('.notification');

    existingNotifications.forEach(existingNotification => {
        existingNotification.style.bottom = parseInt(existingNotification.style.bottom) + 50 + 'px';
    });

    notificationContainer.appendChild(notification);
    notification.style.bottom = '0';

    // Добавляем класс "show" для анимации появления
    setTimeout(() => {
        notification.classList.add('show');
    }, 0);

    // Задержка перед началом анимации исчезновения
    setTimeout(() => {
        notification.classList.add('hide');

        // Удаление уведомления после окончания анимации
        notification.addEventListener('transitionend', () => {
            notification.remove();
        });
    }, 3000); // Изменено на 3000 миллисекунд для удобства демонстрации

}


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

function validateStreamLogin(login) {
    var loginPattern = /^[a-zA-Z0-9_-]+$/;
    var isValidLength = login.length >= 4 && login.length <= 16; // Минимальное и максимальное количество символов
    var isValidPattern = loginPattern.test(login);

    if (isValidLength && isValidPattern) {
        return true;
    }
    // Невалидный
    showNotification("Некорректное значение поля. Проверьте правильность ввода.")
    return false;
}


$(document).ready(function () {
    spamButton.addEventListener('click', function () {
        updateUnderline(spamButton);
        spamButton.classList.add("button-active");
        spamButton.disabled = true;
        bindsButton.classList.remove("button-active");
        bindsButton.disabled = false;
        manualButton.classList.remove("button-active");
        manualButton.disabled = false;
        loadContent("Spam");
    });

    bindsButton.addEventListener('click', function () {
        updateUnderline(bindsButton);
        bindsButton.classList.add("button-active");
        bindsButton.disabled = true;
        spamButton.classList.remove("button-active");
        spamButton.disabled = false;
        manualButton.classList.remove("button-active");
        manualButton.disabled = false;
        loadContent("Binds");
    });

    manualButton.addEventListener('click', function () {
        updateUnderline(manualButton);
        manualButton.classList.add("button-active");
        manualButton.disabled = true;
        bindsButton.classList.remove("button-active");
        bindsButton.disabled = false;
        spamButton.classList.remove("button-active");
        spamButton.disabled = false;
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

    $('.streamer-username-container > button').on('click', async function () {
        var  $input = $(this).siblings('input');
        var $img = $(this).find('img');
        if ($input.attr('disabled')) {
            $input.removeAttr('disabled');
            $img.attr('src', '/App/save.svg');
            return;
        }
        var text = $input.val();
        if (!validateStreamLogin(text)) {
            return;
        }
        var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");
        try {
            await $.ajax({
                url: 'api/app/updateStreamerUsername?username=' + text,
                type: 'PUT',
                headers: {
                    "Authorization": auth_token
                },
            });
            $input.prop("disabled", true);
            $img.attr('src', '/App/edit.svg');

        } catch (error) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        }
    });
    $('.streamer-username-container > input').on('keypress', function (e) {
        if (e.which === 13) {
            $(this).siblings('button').click();
        }
    });


    spamButton.click();
});
