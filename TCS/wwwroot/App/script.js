const spamButton = document.getElementById('spam-btn');
const bindsButton = document.getElementById('binds-btn');
const manualButton = document.getElementById('manual-btn');
const underline = document.getElementById('underline');
const notificationContainer = document.getElementById('notification-container');
var streamerUsername = $('.streamer-username-container > input').val();
const $masSelectedOption = $('#func-select > .selected-option');
const $masOptions = $('#func-select > .options');
var avalibleMasClick = true;
const history = [];
var historyIndex = 0;
var isRandom = false;
var lastMessage = "";

function scrollBotList() {
    var targetElement = document.getElementById('bots'); // Получите контейнер по его ID
    var newSelectedBotItem = document.querySelector('#bots-list #bots div[class="item selected-item"]'); // Получите элемент, до которого хотите проскроллить, по его ID
    var elementRect = newSelectedBotItem.getBoundingClientRect();
    var containerRect = targetElement.getBoundingClientRect();

    if (
        elementRect.top < containerRect.top ||
        elementRect.bottom > containerRect.bottom
    ) {
        // Элемент не полностью виден, прокручиваем к нему
        var elementTopRelativeToContainer = elementRect.top - containerRect.top;
        targetElement.scrollTop += elementTopRelativeToContainer;
    }

}

function nextBtn() {
    var selectedBotname = history[historyIndex];
    var selectedBotItem = $('#bots-list div[botname="' + selectedBotname + '"]');
    if (selectedBotItem.length === 0) {
        return;
    }
    selectedBotItem.removeClass('selected-item');
    if (history.length == historyIndex + 1) {
        if (isRandom) {
            var newSelectedBotItem;
            if ($('#bots-list #bots [state="connected"]').length == 0) {
                newSelectedBotItem = selectedBotItem;
            } else {
                do {
                    var randomIndex = Math.floor(Math.random() * $('#bots-list #bots [state="connected"]').length);
                    newSelectedBotItem = $($('#bots-list #bots [state="connected"]')[randomIndex])
                } while (newSelectedBotItem.is(selectedBotItem) && !$('#bots-list #bots [state="connected"]').length == 1);

            }
            var newSelectedBotname = newSelectedBotItem.attr('botname');
        }
        else if ($("#bots-list #bots div:last").is(selectedBotItem)) {
            var newSelectedBotItem = $('#bots-list #bots :first');
            var newSelectedBotname = newSelectedBotItem.attr('botname');
        }
        else {
            var newSelectedBotItem = selectedBotItem.next();
            var newSelectedBotname = newSelectedBotItem.attr('botname');
        }
        newSelectedBotItem.addClass('selected-item');
        history.push(newSelectedBotname);
        if (history.length > 10) {
            history.shift();
            historyIndex = 9;
        }
        else if (history.length == 1) {
            historyIndex = 0;
        }
        else {
            historyIndex++;
        }
    }
    else {
        var newSelectedBotItem = $('#bots-list div[botname="' + history[historyIndex] + '"]');
        newSelectedBotItem.addClass('selected-item');
        historyIndex++;
    }
    scrollBotList();

}

function prevBtn() {
    if (historyIndex === 0 || history.length == 0) {
        return;
    }
    var selectedBotname = history[historyIndex];
    var selectedBotItem = $('#bots-list div[botname="' + selectedBotname + '"]');
    selectedBotItem.removeClass('selected-item');
    var newSelectedBotItem = $('#bots-list div[botname="' + history[historyIndex - 1] + '"]');
    newSelectedBotItem.addClass('selected-item');
    historyIndex--;
    scrollBotList();

}

function randomBtn() {
    isRandom = !isRandom;
    if (isRandom) {
        $('#random-btn > img').attr('src', '/App/random_active.svg');
        return;
    }
    $('#random-btn > img').attr('src', '/App/random.svg');
}

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

async function loadContent(partialViewName) {
    try {
        var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

        const response = await fetch(`/App/LoadPartialView?partialViewName=${partialViewName}`, {
            method: 'GET',
            headers: {
                'Authorization': auth_token
            }
        });

        if (response.ok) {
            const data = await response.text();
            $("#content").html(data);
        } else {
            throw new Error();
        }
    } catch (error) {
        // Обработка ошибки
        window.location.href = '/';
    }
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

function pingPong() {
    var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");
    fetch("api/app/ping", {
        method: "GET",
        headers: {
            "Authorization": auth_token
        }
    });
}

async function toggleBot() {
    if (!avalibleMasClick) {
        return;
    }
    const $div = $(this.parentNode);
    const state = $div.attr('state');
    const botname = $div.attr('botname');
    const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    try {
        if (state === 'connected') {
            const response = await fetch("/api/app/disconnectBot", {
                method: "POST",
                headers: {
                    "Authorization": auth_token,
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    botusername: botname
                })
            });

            if (!response.ok) {
                throw Error();
            }

            const data = await response.json();

            if (data.status === "ok") {
                $div.attr('state', 'disconnected');
                const $img = $div.find('img');
                $img.attr('src', '/App/connect_bot.svg');
                return;
            }

            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        } else {
            const response = await fetch("/api/app/connectBot", {
                method: "POST",
                headers: {
                    "Authorization": auth_token,
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    botusername: botname
                })
            });

            if (!response.ok) {
                throw Error();
            }

            const data = await response.json();

            if (data.status === "ok") {
                $div.attr('state', 'connected');
                const $img = $div.find('img');
                $img.attr('src', '/App/disconnect_bot.svg');
                return;
            }

            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        }
    } catch (error) {
        showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
    }
}

function connectAllBots() {
    var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");
    showNotification("Подключение всех ботов... Пожалуйста не перезагружайте страницу.");
    fetch("/api/app/connectAllBots", {
        method: "POST",
        headers: {
            "Authorization": auth_token
        }
    })
        .then(response => {
            if (!response.ok) {
                throw Error();
            }
            return response.json();
        })
        .then(data => {
            avalibleMasClick = true;

            if (data.status === "ok") {
                var $botsContainer = $('#bots');
                $botsContainer.find('img').attr('src', '/App/disconnect_bot.svg');
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
                        Object.keys(data).forEach(function (key) {
                            let value = data[key];
                            $div = $botsContainer.find('div[botname="' + key + '"]');
                            $div.attr('state', (value ? 'connected' : 'disconnected'));
                            $img = $div.find('img');
                            $img.attr({
                                src: value ? '/App/disconnect_bot.svg' : '/App/connect_bot.svg',
                                alt: ''
                            });
                        });
                    })
                    .catch(error => {
                        // Обработка ошибки
                    });

                showNotification("Все боты успешно подключены.");
                return;
            }
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        })
        .catch(error => {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        });
}

function disconnectAllBots() {
    var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");
    showNotification("Отключение всех ботов... Пожалуйста не перезагружайте страницу.");

    fetch("/api/app/disconnectAllBots", {
        method: "POST",
        headers: {
            "Authorization": auth_token
        }
    })
        .then(response => {
            if (!response.ok) {
                throw Error();
            }
            return response.json();
        })
        .then(data => {
            avalibleMasClick = true;

            if (data.status === "ok") {
                var $botsContainer = $('#bots');
                $botsContainer.find('div[botname]').attr('state', 'disconnected');
                $botsContainer.find('img').attr('src', '/App/connect_bot.svg');
                showNotification("Все боты успешно отключены.");
                return;
            }
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        })
        .catch(error => {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        });
}


$(document).ready(function () {
    spamButton.addEventListener('click', async function () {
        await loadContent("Spam");
        updateUnderline(spamButton);
        spamButton.classList.add("button-active");
        spamButton.disabled = true;
        bindsButton.classList.remove("button-active");
        bindsButton.disabled = false;
        manualButton.classList.remove("button-active");
        manualButton.disabled = false;
    });

    bindsButton.addEventListener('click', async function () {
        await loadContent("Binds");
        updateUnderline(bindsButton);
        bindsButton.classList.add("button-active");
        bindsButton.disabled = true;
        spamButton.classList.remove("button-active");
        spamButton.disabled = false;
        manualButton.classList.remove("button-active");
        manualButton.disabled = false;
    });

    manualButton.addEventListener('click', async function () {
        await loadContent("Manual");
        updateUnderline(manualButton);
        manualButton.classList.add("button-active");
        manualButton.disabled = true;
        bindsButton.classList.remove("button-active");
        bindsButton.disabled = false;
        spamButton.classList.remove("button-active");
        spamButton.disabled = false;
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
                var $botsContainer = $('#bots');
                var isConnected = $botsContainer.find('div[state="connected"]').length > 0;
                if (isConnected) {
                    showNotification("Отключение всех ботов... Пожалуйста не перезагружайте страницу.");
                }
                $(this).prop("disabled", true);
                await fetch('api/app/updateStreamerUsername?username=' + text, {
                    method: 'PUT',
                    headers: {
                        'Authorization': auth_token,
                    }
                });
                $(this).prop("disabled", false);

                if (isConnected) {
                    showNotification("Все боты успешно отключены.");
                    var spam_btn = $('#s_start-stop');
                    if (spam_btn) {
                        spam_btn.attr('state', 'stopped');
                        spam_btn.text('Начать');
                    }
                    $botsContainer.find('div[botname]').attr('state', 'disconnected');
                    $botsContainer.find('img').attr('src', '/App/connect_bot.svg');
                }

                $('#status-streamerusername').text(text);
                streamerUsername = text;
                onlineStreamerCheck();
                $('#stream-content > iframe:nth-child(1)').attr('src', 'https://player.twitch.tv/?channel=' + streamerUsername + '&parent=rbtchat.site');
                $('#stream-content > iframe:nth-child(2)').attr('src', 'https://www.twitch.tv/embed/' + streamerUsername + '/chat?darkpopout&parent=rbtchat.site');
            }
            $input.prop("disabled", true);
            $img.attr('src', '/App/edit.svg');

        } catch (error) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        }
    });
    $('.streamer-username-container > input').on('keypress', function (e) {
        var $button = $(this).siblings('button');
        if (e.which === 13 && !$button.attr('disabled')) {
            $button.click();
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
        try {
            let $optionSelectedOption = $('#b_option_select> .selected-option');
            let $optionOptions = $('#b_option_select > .options');
            if (!$(event.target).is($optionSelectedOption) && !$.contains($optionOptions[0], event.target) && $optionOptions.is(':visible')) {
                toggleOptionsMenu();
            }
        } catch { }
        try {
            let $optionSelectedOption = $('#b_edit_button_select > div');
            let $optionOptions = $('#b_edit_button_select > ul');
            if (!$(event.target).is($optionSelectedOption) && !$.contains($optionOptions[0], event.target) && $optionOptions.is(':visible')) {
                toggleEditBindsMenu();
            }
        } catch { }
        try {
            var $optionSelectedOption = $('#b_delete_button_select > div');
            var $optionOptions = $('#b_delete_button_select > ul');
            if (!$(event.target).is($optionSelectedOption) && !$.contains($optionOptions[0], event.target) && $optionOptions.is(':visible')) {
                toggleDeleteBindsMenu();
            }
        } catch { }

    });
    $('#mas-connect').on('click', function () {
        toggleMass();

        if (!avalibleMasClick || $('#bots div[state="disconnected"]').length == 0) {
            return;
        }
        avalibleMasClick = false;
        connectAllBots();
    });
    $('#mas-disconnect').on('click', function () {
        toggleMass();

        if (!avalibleMasClick || $('#bots div[state="connected"]').length == 0) {
            return;
        }
        avalibleMasClick = false;
        disconnectAllBots();
    });

    pingPong();
    onlineStreamerCheck();
    setInterval(onlineStreamerCheck, 10000);
    setInterval(pingPong, 10000);
    manualButton.click();
    //getBots();
    let items = $('#bots-list #bots div[botname]');
    items.on('click', function () {
        var thisBot = $(this);
        $('#bots-list div[botname]').removeClass('selected-item');
        thisBot.addClass('selected-item');
        history.push(thisBot.attr('botname'));
        if (history.length > 10) {
            history.shift();
            historyIndex = 9;
        }
        else if (history.length == 1) {
            historyIndex = 0;
        }
        else {
            historyIndex++;
        }
    });
    items.find('button').on('click', toggleBot);
    items.first().click();


    $('#next-btn').on('click', nextBtn);
    $('#prev-btn').on('click', prevBtn);
    $('#random-btn').on('click', randomBtn);
});
