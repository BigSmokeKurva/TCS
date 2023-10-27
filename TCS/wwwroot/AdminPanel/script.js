let selectedButton = null;
let users = {};
let selectedUser = null;
let selectedUserInfo = null;
const search = $('.search > input');
const $logsSelectedOption = $('#logs-select > .selected-option');
const $logsOptions = $('#logs-select > .options');
const notificationContainer = document.getElementById('notification-container');
const propertyMappings = {
    'username': 0,
    'password': 1,
    'email': 2,
    'admin': 3,
    'tokens': 4,
    'proxies': 5
};

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

async function getInfoForSelectedUser() {
    const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    try {
        const response = await fetch('api/admin/getuserinfo?id=' + selectedUser.id, {
            method: 'GET',
            headers: {
                "Authorization": auth_token
            }
        });

        if (!response.ok) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            return null; // или верните значение по умолчанию
        }

        const data = await response.json();
        return data;
    } catch (error) {
        showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        return null; // или верните значение по умолчанию
    }
}

function getLogs() {
    var selectedTime = $logsSelectedOption.find('span').text();
    var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    fetch('api/admin/getlogs?id=' + selectedUser.id + '&time=' + selectedTime, {
        method: 'GET',
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
        .then(logs => {
            var logsContainer = $("#logs > div.list.list-logs");
            logsContainer.empty();

            logs.forEach(log => {
                var logTime = new Date(log.time);
                var formattedTime = logTime.toLocaleTimeString();

                var logDiv = $("<div>").addClass("logs_line");
                var timeSpan = $("<span>").text(formattedTime);
                var messageSpan = $("<span>").text(log.message);

                logDiv.append(timeSpan, messageSpan);
                logsContainer.append(logDiv);

                var underlineDiv = $("<div>").addClass("logs_underline");
                logsContainer.append(underlineDiv);
            });
        })
        .catch(error => {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        });
}

async function selectUser(button) {
    if (selectedButton) {
        selectedButton.removeAttribute("style");
        selectedButton.disabled = false;
    }
    button.style.backgroundColor = "rgb(118, 104, 203)";
    button.disabled = true;
    selectedButton = button;

    var user_id = parseInt(button.getAttribute("user_id"));
    selectedUser = users[user_id];
    selectedUserInfo = await getInfoForSelectedUser();

    // Используем тернарный оператор для определения значения streamerUsername
    $('#streamer-username').text(selectedUserInfo.streamerUsername ? selectedUserInfo.streamerUsername : "-");
    $('#user-username').text(selectedUser.username);
    $('#password').text(selectedUserInfo.password);
    $('#tokens-count').text(selectedUserInfo.tokensCount);
    $('#proxies-count').text(selectedUserInfo.proxiesCount);
    $('#email').text(selectedUser.email);
    $("#checkbox").prop("checked", selectedUserInfo.admin);

    const logsTimeList = $('#logs-time-list');
    logsTimeList.empty();

    // Используем метод `map` для создания массива элементов <li>
    const liElements = selectedUserInfo.logsTime.map(timestamp => $('<li>').text(timestamp));
    logsTimeList.append(liElements);

    // Создаем jQuery-коллекцию из массива элементов и добавляем обработчик событий
    logsTimeList.on('click', 'li', function () {
        logsTimeList.find('li.selected').removeClass('selected');
        $(this).addClass('selected');
        $logsSelectedOption.find('span').text($(this).text());
        toggleLogs();
        getLogs();
    });

    // Выбираем первый элемент и обновляем отображение
    const firstLi = logsTimeList.find('li:first');
    firstLi.addClass('selected');
    $logsSelectedOption.find('span').text(firstLi.text());
    getLogs();
    $("#info-grid").show();

}

function getUsers() {
    var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    fetch('api/admin/getusers', {
        method: 'GET',
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
            var usersList = $('#users.list');
            usersList.empty();

            data.forEach(user => {
                var btn = $('<button>', { user_id: user.id }).text(user.username).on('click', function () {
                    selectUser(this);
                });
                users[user.id] = user;

                var item = $('<div>').addClass('item').append(btn);
                usersList.append(item);
            });
        })
        .catch(error => {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        });
}

function searchUser() {
    var searchText = search.val().toLowerCase();


    if (searchText === "") {
        $('.item > button[style="display: none;"]').show();
        return;
    }
    $('.item > button').hide();

    var matchingKeys = [];

    for (var key in users) {
        if (users.hasOwnProperty(key)) {
            var user = users[key];
            var username = user.username.toLowerCase();
            var email = user.email.toLowerCase();

            if (username.includes(searchText) || email.includes(searchText)) {
                matchingKeys.push(key);
            }
        }
    }

    // Показать только кнопки с соответствующими user_id
    matchingKeys.forEach(function (userId) {
        $('.item > button[user_id="' + userId + '"]').show();
    });

}

function toggleLogs() {
    $logsOptions.is(':visible') ? $logsOptions.fadeOut(100) : $logsOptions.fadeIn(100);
    $logsSelectedOption.toggleClass('selected-option-active');
    $logsSelectedOption.find('div > img').toggleClass('triangle-open triangle-close');
}

async function sendEditUserRequest(property, value) {
    property = propertyMappings[property];
    var data = {
        id: selectedUser.id,
        property: property,
        value: value
    };
    var auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    try {
        const response = await fetch('api/admin/edituser', {
            method: 'POST',
            headers: {
                "Authorization": auth_token,
                "Content-Type": "application/json"
            },
            body: JSON.stringify(data)
        });

        if (!response.ok) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            return false;
        }

        const responseData = await response.json();
        if (responseData.status !== 'ok') {
            showNotification(responseData.message);
            return false;
        }

        return responseData;
    } catch (error) {
        // Обработка ошибки
        showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        return false;
    }
}

async function editUserEditable(selector, property) {
    var input = $(selector);
    var img = input.siblings('button').find('img');

    if (input.attr('contenteditable') === 'true') {
        var value = input.text();
        if (!await sendEditUserRequest(property, value)) {
            return false;
        }
        input.attr('contenteditable', 'false');
        img.attr('src', '/App/edit.svg');
        return true;
    }

    input.attr('contenteditable', 'true');
    img.attr('src', '/App/save.svg');
    return true;
}

function uploadTokens(input) {
    var selectedFile = input[0].files[0];
    if (!selectedFile) {
        return;
    }

    var reader = new FileReader();

    reader.onload = async function (e) {
        var fileContent = e.target.result;
        var lines = fileContent.split('\n')
            .map(line => line.trim().replace(/\r/g, ''))
            .filter(line => line !== '');

        try {
            showNotification("Проверка токенов...");
            var response = await sendEditUserRequest('tokens', lines);
            $('#tokens-count').text(response.message);
            showNotification("Токены проверены.");
        } catch (error) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        }
    };

    reader.readAsText(selectedFile);
}

function uploadProxies(input) {
    var selectedFile = input[0].files[0];
    if (!selectedFile) {
        return;
    }

    var reader = new FileReader();

    reader.onload = async function (e) {
        var fileContent = e.target.result;
        var lines = fileContent.split('\n')
            .map(line => line.trim().replace(/\r/g, ''))
            .filter(line => line !== '');

        try {
            var response = await sendEditUserRequest('proxies', lines);
            $('#proxies-count').text(response.message);
        } catch (error) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        }
    };

    reader.readAsText(selectedFile);
}

function download(data, fileName) {
    // Создаем новый Blob (бинарный объект) с вашими данными и типом MIME "text/plain"
    const blob = new Blob([data], { type: "text/plain" });

    // Создаем ссылку для скачивания
    const url = window.URL.createObjectURL(blob);

    // Создаем элемент "a" для скачивания файла
    const a = $("<a>")
        .attr("href", url)
        .attr("download", fileName); // Имя файла, которое будет предложено при скачивании

    // Добавляем элемент "a" в документ и производим клик на нем для начала загрузки
    a.appendTo("body").get(0).click();

    // Освобождаем ресурсы после завершения скачивания
    window.URL.revokeObjectURL(url);

    // Удаляем элемент "a" после скачивания
    a.remove();
}

async function downloadTokens() {
    const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    try {
        const response = await fetch('api/admin/gettokens?id=' + selectedUser.id, {
            method: 'GET',
            headers: {
                "Authorization": auth_token
            }
        });

        if (!response.ok) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            return;
        }
        const data = await response.json();

        let resultString = "";

        // Перебираем элементы словаря
        for (const key in data) {
            if (data.hasOwnProperty(key)) {
                resultString += key + ":" + data[key] + "\n";
            }
        }

        download(resultString, 'tokensAndUsernames.txt');
        resultString = "";

        // Перебираем элементы словаря
        for (const key in data) {
            if (data.hasOwnProperty(key)) {
                resultString += key + "\n";
            }
        }

        download(resultString, 'tokens.txt');

    } catch (error) {
        showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
    }
}

async function downloadProxies() {
    const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

    try {
        const response = await fetch('api/admin/getproxies?id=' + selectedUser.id, {
            method: 'GET',
            headers: {
                "Authorization": auth_token
            }
        });

        if (!response.ok) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
            return;
        }

        const data = await response.json();

        let resultString = "";

        data.forEach(proxy => {
            resultString += proxy + "\n";
        });

        download(resultString, 'proxies.txt');
    } catch (error) {
        showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
    }
}

function setupFileInput(input, uploadFunction) {
    input.off('change');
    input.change(function () {
        uploadFunction(input);
    });
    input.click();
    input.val('');
}

$(document).ready(function () {
    getUsers();
    search.on('input', searchUser);
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
    $logsSelectedOption.on('click', function (e) {
        e.stopPropagation();
        toggleLogs();
    });
    $(document).on('click', function (event) {
        if (!$(event.target).is($logsSelectedOption) && !$.contains($logsOptions[0], event.target) && $logsOptions.is(':visible')) {
            toggleLogs();
        }
    });
    $('#email + button').on('click', async function () {
        if (await editUserEditable('#email', 'email')) {
            selectedUser.email = $('#email').text();
            users[selectedUser.id].email = selectedUser.email;
        }
    });

    $('#user-username + button').on('click', async function () {
        if (await editUserEditable('#user-username', 'username')) {
            selectedUser.username = $('#user-username').text();
            users[selectedUser.id].username = selectedUser.username;
            $('#users > div > button[disabled]').text(selectedUser.username);
        }
    });

    $('#password + button').on('click', function () {
        editUserEditable('#password', 'password');
        selectedUserInfo.password = $('#password').text();
    });
    $('#info-panel span[contenteditable]').on('keydown', function (e) {
        if (e.keyCode == 13) {
            e.preventDefault();
            $(this).find('+ button').click();
        }
    })
    $('#checkbox').on('click', function () {
        var isChecked = this.checked;
        sendEditUserRequest('admin', isChecked);
        selectedUserInfo.admin = isChecked;
    });
    $('#upload-tokens').on('click', function () {
        setupFileInput($('#fileInput'), uploadTokens);
    });

    $('#upload-proxies').on('click', function () {
        setupFileInput($('#fileInput'), uploadProxies);
    });

    $('#download-tokens').on('click', function () {
        downloadTokens();
    });

    $('#download-proxies').on('click', function () {
        downloadProxies();
    });

    $('.delete-account-button').on('click', async function () {
        const auth_token = document.cookie.replace(/(?:(?:^|.*;\s*)auth_token\s*=\s*([^;]*).*$)|^.*$/, "$1");

        try {
            const response = await fetch('api/admin/deleteuser?id=' + selectedUser.id, {
                method: 'DELETE',
                headers: {
                    "Authorization": auth_token
                }
            });

            if (!response.ok) {
                showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
                return;
            }

            const data = await response.json();

            if (data.status !== 'ok') {
                throw new Error();
            }

            $('#users > div > button[disabled]').remove();
            delete users[selectedUser.id];
            selectedUserInfo = null;
            selectedUser = null;
            $("#info-grid").hide();
        } catch (error) {
            showNotification("Произошла неизвестная ошибка. Попробуйте позже.");
        }
    });
});


