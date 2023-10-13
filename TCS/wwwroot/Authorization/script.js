const loginButton = document.getElementById('login-header');
const registerButton = document.getElementById('register-header');
const underline = document.getElementById('underline');
const notificationContainer = document.getElementById('notification-container');

function updateUnderline(element) {
    const buttonRect = element.getBoundingClientRect();
    const containerRect = document.querySelector('.button-container').getBoundingClientRect();
    const leftOffset = buttonRect.left - containerRect.left;

    underline.style.width = buttonRect.width + 32 + 'px';
    underline.style.left = leftOffset - 16 + 'px';
}

function clickAuth() {
    if (!(validateLogin() | validatePassword()))
        return;
    var login = $('#login').val();
    var password = $('#password').val();

    var requestData = {
        login: login,
        password: password
    };

    $.ajax({
        type: "POST",
        url: "/api/authorization",
        data: JSON.stringify(requestData), // Преобразуем данные в JSON
        contentType: "application/json", // Устанавливаем заголовок Content-Type
        success: function (response) {
            // Обработка успешного ответа от сервера
            if (response.status == "error") {
                showNotification(response.message);
                return;
            }
            window.location.href = "/";
        },
        error: function (error) {
            // Обработка ошибки
            showNotification("Произошла ошибка при авторизации. Попробуйте позже.");
        }
    });

}

function clickReg() {
    if (!(validateEmail() | validateLogin() | validatePassword()))
        return;

    var email = $('#email').val();
    var login = $('#login').val();
    var password = $('#password').val();

    var requestData = {
        email: email,
        login: login,
        password: password
    };

    $.ajax({
        type: "POST",
        url: "/api/registration",
        data: JSON.stringify(requestData), // Преобразуем данные в JSON
        contentType: "application/json", // Устанавливаем заголовок Content-Type
        success: function (response) {
            // Обработка успешного ответа от сервера
            if (response.status == "error") {
                showNotification(response.message);
                return;
            }
            window.location.href = "/";
        },
        error: function (error) {
            // Обработка ошибки
            showNotification("Произошла ошибка при регистрации. Попробуйте позже.");
        }
    });
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

function validateEmail() {
    var email = $('#email').val();
    var emailPattern = /^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$/;
    var isValidLength = email.length <= 30;
    var isValidPattern = emailPattern.test(email);

    // Конфигурация для управления элементами
    var config = {
        continueButton: $('.continue-button'),
        emailInput: $('#email')
    };

    if (isValidLength && isValidPattern) {
        // Валидный
        config.continueButton.prop('disabled', false);
        config.emailInput.removeClass('invalid-input');
        return true;
    }
    // Невалидный
    config.continueButton.prop('disabled', true);
    config.emailInput.addClass('invalid-input');
    return false;
}

function validateLogin() {
    var login = $('#login').val();
    var loginPattern = /^[a-zA-Z0-9_-]+$/;
    var isValidLength = login.length >= 4 && login.length <= 12; // Минимальное и максимальное количество символов
    var isValidPattern = loginPattern.test(login);

    // Конфигурация для управления элементами
    var config = {
        continueButton: $('.continue-button'),
        loginInput: $('#login')
    };

    if (isValidLength && isValidPattern) {
        // Валидный
        config.continueButton.prop('disabled', false);
        config.loginInput.removeClass('invalid-input');
        return true;
    }
    // Невалидный
    config.continueButton.prop('disabled', true);
    config.loginInput.addClass('invalid-input');
    return false;
}

function validatePassword() {
    var password = $('#password').val();
    var minLength = 5; // Минимальная длина пароля
    var maxLength = 30; // Максимальная длина пароля
    var passwordPattern = /^[a-zA-Z0-9!@#$%^&*()_-]+$/;
    var isValid = password.length >= minLength && password.length <= maxLength && passwordPattern.test(password);

    // Конфигурация для управления элементами
    var config = {
        continueButton: $('.continue-button'),
        passwordInput: $('#password')
    };

    if (isValid) {
        // Валидный
        config.continueButton.prop('disabled', false);
        config.passwordInput.removeClass('invalid-input');
        return true;
    }
    // Невалидный
    config.continueButton.prop('disabled', true);
    config.passwordInput.addClass('invalid-input');
    return false;
}

loginButton.addEventListener('click', () => {
    updateUnderline(loginButton);
    loginButton.classList.add("button-active");
    registerButton.classList.remove("button-active");
    var form =
        $('<input>', { id: 'login', placeholder: 'логин' }).on('blur', validateLogin).add(
            $('<input>', { id: 'password', placeholder: 'пароль', type: 'password' }).on('blur', validatePassword)).add(
                $('<button>', { class: 'continue-button', text: 'ВОЙТИ' }).click(
                    clickAuth
                )
            );

    $('#main-form').html(form);
});

registerButton.addEventListener('click', () => {
    updateUnderline(registerButton);
    registerButton.classList.add("button-active");
    loginButton.classList.remove("button-active");
    var form =
        $('<input>', { id: 'email', placeholder: 'почта' }).on('blur', validateEmail).add(
            $('<input>', { id: 'login', placeholder: 'логин' }).on('blur', validateLogin)).add(
                $('<input>', { id: 'password', placeholder: 'пароль', type: 'password' }).on('blur', validatePassword)).add(
                    $('<button>', { class: 'continue-button', text: 'РЕГИСТРАЦИЯ' }).click(
                        clickReg
                    )
                );

    $('#main-form').html(form);
});

// Обновляем подчеркивание при загрузке страницы.
window.addEventListener('load', () => {
    loginButton.click();
});
