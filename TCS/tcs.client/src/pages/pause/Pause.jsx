import styles from "./style.module.css";
import pauseSvg from "../../assets/pause.svg"

function Pause(){
    return(
        <div className={styles.main_container}>
            <img src={pauseSvg} alt="pause" />
            <span>Аккаунт приостановлен</span>
        </div>
    )
}

export default Pause;