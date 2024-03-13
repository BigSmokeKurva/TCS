import { useState, useRef, useEffect, useContext, useCallback } from "react";
import styles from "../../shared_components/DropdownContainer.module.css";
import _styles from "./BindsDropdownContainer.module.css";
import classNames from "classnames";
import { EditorContext } from "../../admin_panel/contexts/EditorContext";
import TokensDropdown from "../../admin_panel/components/TokensDropdown";
import Cookies from "js-cookie";
import { NotificationsContext } from "../../../contexts/notification/NotificationsContext";

function BotsDropdownContainer({ children, title, _isOpen, disabled, buttonStyle, callbackFunc }) {
    const [isOpen, setIsOpen] = useState(_isOpen !== undefined ? _isOpen : false);
    const containerRef = useRef(null);
    const buttonRef = useRef(null);
    const contentRef = useRef(null);
    const { openEditor } = useContext(EditorContext);
    const { showNotification } = useContext(NotificationsContext);

    useEffect(() => {
        if (isOpen) {
            containerRef.current.style.removeProperty("height");
        }
    }, [isOpen])

    useEffect(() => {
        if (!isOpen && !disabled) {
            containerRef.current.style.height = buttonRef.current.offsetHeight + "px";
        }
    }, [])

    const openEditorTokens = useCallback(async () => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/gettokens?usernames=true", {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            }
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        const result = await response.json();

        if (result.status === "error") {
            showNotification(result.message, "error");
            return;
        }

        openEditor({
            _editorData: {
                title: "Токены",
                text: result.join('\n'),
                callback: saveEditorTokens
            }
        });
    });

    const saveEditorTokens = useCallback(async (text) => {
        showNotification("Проверка токенов...", "warning");
        var auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/uploadTokens", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            },
            body: JSON.stringify(text.split('\n'))
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        const result = await response.json();

        if (result.status !== "ok") {
            showNotification(result.message, "error");
            return;
        }
        showNotification("Токены загружены", "success");

        callbackFunc();
    });

    return (
        disabled ? (
            <div
                className={classNames(styles.dropdown_container, _styles.dropdown_container, !isOpen && styles.closed)}
                ref={containerRef}
            >
                <div ref={buttonRef} style={buttonStyle && buttonStyle} disabled={disabled}>
                    <span>{title}</span>
                    <TokensDropdown items={[
                        { title: "Редактировать", callback: openEditorTokens }
                    ]} />
                    <svg xmlns="http://www.w3.org/2000/svg" width="21" height="21" viewBox="0 0 21 21" fill="none">
                        <path d="M15 14.1001L10.5 9.6001L6 14.1001L4.5 12.6001L10.5 6.6001L16.5 12.6001L15 14.1001Z" fill="#EFEFF1" />
                    </svg>
                </div>
                {children}
            </div>
        ) : (
            <div
                className={classNames(styles.dropdown_container, _styles.dropdown_container, !isOpen && _styles.closed)}
                ref={containerRef}
                onTransitionEnd={() => {
                    if (!isOpen) {
                        containerRef.current.style.height = buttonRef.current.offsetHeight + "px";
                    }
                }}
            >
                <div ref={buttonRef} onClick={() => setIsOpen(!isOpen)}>
                    <span>{title}</span>
                    <TokensDropdown items={[
                        { title: "Редактировать", callback: openEditorTokens }
                    ]} />
                    <svg xmlns="http://www.w3.org/2000/svg" width="21" height="21" viewBox="0 0 21 21" fill="none">
                        <path d="M15 14.1001L10.5 9.6001L6 14.1001L4.5 12.6001L10.5 6.6001L16.5 12.6001L15 14.1001Z" fill="#EFEFF1" />
                    </svg>
                </div>
                <div ref={contentRef}>{children}</div>
            </div>
        )
    );
}

export default BotsDropdownContainer;
