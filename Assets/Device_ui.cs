using UnityEngine;
using UnityEngine.UI;

public class Device_ui : MonoBehaviour
{
    public Text txt_title;
    public Text txt_tip;

    private int count_session=0;
    public void Add_session(){
        count_session++;
        this.Check_info();
    }

    public void Reset(){
        this.count_session=0;
        this.Check_info();
    }

    private void Check_info(){
        this.txt_tip.text="Đã chạy được "+count_session+" phiên";
    }
}
