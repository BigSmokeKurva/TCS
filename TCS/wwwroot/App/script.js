const spamButton = document.getElementById('spam-btn');
const bindsButton = document.getElementById('binds-btn');
const manualButton = document.getElementById('manual-btn');
const underline = document.getElementById('underline');
const notificationContainer = document.getElementById('notification-container');
var streamerUsername = $('.streamer-username-container > input').val();
const $masSelectedOption = $('#func-select > .selected-option');
const $masOptions = $('#func-select > .options');


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

    fetch(`/App/LoadPartialView?partialViewName=${partialViewName}`, {
        method: 'GET',
        headers: {
            'Authorization': auth_token
        }
    })
        .then(response => {
            if (response.ok) {
                return response.text(); // Используем метод .json() для парсинга JSON
            } else {
                throw new Error();
            }
        })
        .then(data => {
            $("#content").html(data);
        })
        .catch(error => {
            // Обработка ошибки
            window.location.href = '/';
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

function onlineStreamerCheck() {
    if (!streamerUsername || streamerUsername === 'не указан') {
        return;
    }

    var online = $('#status-streameronline');

    var requestData = [
        {
            "operationName": "UseLive",
            "variables": {
                "channelLogin": streamerUsername
            },
            "extensions": {
                "persistedQuery": {
                    "version": 1,
                    "sha256Hash": "639d5f11bfb8bf3053b424d9ef650d04c4ebb7d94711d644afb08fe9a0fad5d9"
                }
            }
        }
    ];

    fetch("https://gql.twitch.tv/gql", {
        method: "POST",
        headers: {
            "Client-Id": "kimne78kx3ncx6brgo4mv6wki5h1ko",
            "Content-Type": "application/json"
        },
        body: JSON.stringify(requestData)
    })
        .then(response => {
            if (!response.ok) {
                throw Error();
            }
            return response.json();
        })
        .then(data => {
            if (!data[0].data.user) {
                online.text("ошибка");
                return;
            }
            if (data[0].data.user.stream) {
                online.text("онлайн");
            } else {
                online.text("оффлайн");
            }
        })
        .catch(error => {
            online.text("ошибка");
        });
}

function toggleMass() {
    $masOptions.is(':visible') ? $masOptions.fadeOut(100) : $masOptions.fadeIn(100);
    $masSelectedOption.toggleClass('selected-option-active');
    $masSelectedOption.find('div > img').toggleClass('triangle-open triangle-close');
}

function getBots() {
    var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    fetch("/api/app/getBots", {
        method: "GET",
        headers: {
            "Authorization": auth_token
        }
    })
        .then(response => {
            if (response.ok) {
                return response.json();
            } else {
                throw new Error();
            }
        })
        .then(data => {
            var $botsContainer = $('#bots');
            $botsContainer.empty(); // Очистка содержимого перед добавлением новых данных

            Object.keys(data).forEach(function (key) {
                let value = data[key];
                var $div = $('<div>').addClass('item');
                var $span = $('<span>').text(key);
                var $button = $('<button>');
                var $img = $('<img>').attr({
                    src: value ? '/App/disconnect_bot.svg' : '/App/connect_bot.svg',
                    alt: ''
                });

                $button.append($img);
                $div.append($span, $button);
                $botsContainer.append($div);
            });
        })
        .catch(error => {
            // Обработка ошибки
        });
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
        fetch("/api/auth/unauthorization", {
            method: "GET"
        })
            .then(response => {
                if (!response.ok) {
                    throw Error();
                }
                return response.json();
            })
            .then(data => {
                if (data.status === "ok") {
                    window.location.href = "/";
                }
            })
            .catch(error => {
                // Обработка ошибки
            });
    });

    $('.streamer-username-container > button').on('click', async function () {
        var $input = $(this).siblings('input');
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
            if (text !== streamerUsername) {
                await fetch('api/app/updateStreamerUsername?username=' + text, {
                    method: 'PUT',
                    headers: {
                        'Authorization': auth_token,
                    }
                });
                $('#status-streamerusername').text(text);
                streamerUsername = text;
                onlineStreamerCheck();
                $('#stream-content > iframe:nth-child(1)').attr('src', 'https://player.twitch.tv/?channel=' + streamerUsername + '&parent=localhost');
                $('#stream-content > iframe:nth-child(2)').attr('src', 'https://www.twitch.tv/embed/' + streamerUsername + '/chat?darkpopout&parent=localhost');
            }
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
    $masSelectedOption.on('click', function (e) {
        e.stopPropagation();
        toggleMass();
    });
    $(document).on('click', function (event) {
        if (!$(event.target).is($masSelectedOption) && !$.contains($masOptions[0], event.target) && $masOptions.is(':visible')) {
            toggleMass();
        }
    });


    onlineStreamerCheck();
    setInterval(onlineStreamerCheck, 10000);
    manualButton.click();
    getBots();
});
