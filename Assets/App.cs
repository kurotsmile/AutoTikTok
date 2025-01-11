using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class App : MonoBehaviour
{
    public GameObject device_item_pfefab;
    public Transform tr_all_item;
    List<string> deviceList = new();
    List<Device_ui> device_Uis=new();
    public Sprite sp_stop;
    public Sprite sp_play;

    [Header("Ui")]
    public Text text_btn_play;
    public Text text_txt_limit;
    public Image img_icon_play;
    public Image img_btn_play;
    public InputField inp_session_count;
    public InputField inp_session_timer;
    public InputField inp_action_count;
    public Color32 color_play;
    public Color32 color_stop;

    private bool is_run=false;

    private int leng_action=5;
    private float timer_action=6;
    private int session_count=100;
    private int session_timer=900;

    private int count_cur_session=0;
    void Start()
    {
        this.Load_list_devices();
        this.Check_status_btn_play();

        this.leng_action=PlayerPrefs.GetInt("leng_action",5);
        this.session_count=PlayerPrefs.GetInt("session_count",100);
        this.session_timer=PlayerPrefs.GetInt("session_timer",900);

        this.inp_action_count.text=this.leng_action.ToString();
        this.inp_session_count.text=this.session_count.ToString();
        this.inp_session_timer.text=this.session_timer.ToString();
        this.Check_limit_timer();
    }

    public void Btn_play(){
        if(this.is_run){
            this.is_run=false;
            StopAllCoroutines();
        }else{
            this.count_cur_session=0;
            this.text_btn_play.text="Stop ("+(this.count_cur_session+1)+"/"+this.session_count+")";
            this.is_run=true;
            this.Run_all_action();
            this.session_count=int.Parse(this.inp_session_count.text);
            this.session_timer=int.Parse(this.inp_session_timer.text);
            for(int i=1;i<this.session_count;i++){
                this.CallMethodWithDelay(i*this.session_timer,Run_all_action);
            }
        }
        this.Check_status_btn_play();
    }

    private void Check_status_btn_play(){
        if(this.is_run){
            this.text_btn_play.text="Stop";
            this.img_icon_play.sprite=this.sp_stop;
            this.img_btn_play.color=this.color_stop;
        }else{
            this.text_btn_play.text="Play";
            this.img_icon_play.sprite=this.sp_play;
            this.img_btn_play.color=this.color_play;
        }
    }

    private void Run_all_action(){
        this.text_btn_play.text="Stop ("+(this.count_cur_session+1)+"/"+this.session_count+")";
        this.leng_action=int.Parse(this.inp_action_count.text);
        this.RunADBCommand_All_Device("shell monkey -p com.zhiliaoapp.musically -v 1");

        for(int i=1;i<this.leng_action;i++){
            this.CallMethodWithDelay(i*timer_action,()=>{
                int r_act=Random.Range(0,10);
                int r_x=Random.Range(200,220);
                if(r_act==2){
                    int r_ms=Random.Range(500,1400);
                    this.RunADBCommand_All_Device("shell input swipe "+r_x+" 240 "+r_x+" 800 "+r_ms);
                }else if(r_act==3){
                    this.RunADBCommand_All_Device("shell monkey -p com.zhiliaoapp.musically -v 1");    
                }else if(r_act==4){
                    this.Run_Duble_tap_all_device(300,300,"4");
                }else{
                    int r_ms=Random.Range(500,1400);
                    this.RunADBCommand_All_Device("shell input swipe "+r_x+" 800 "+r_x+" 140 "+r_ms);
                }
            });
        }
        
        this.CallMethodWithDelay(this.leng_action*timer_action,()=>{
            this.RunADBCommand_All_Device("shell am force-stop ccom.zhiliaoapp.musically");
            this.RunADBCommand_All_Device("shell input keyevent KEYCODE_HOME");
            for(int i=0;i<this.device_Uis.Count;i++){
                this.device_Uis[i].Add_session();
            }
            this.count_cur_session++;
            if(this.count_cur_session>=this.session_count){
                this.is_run=false;
                this.Check_status_btn_play();
                StopAllCoroutines();
                this.count_cur_session=0;
            }
        });
    }

    public void ListConnectedDevices(UnityAction<List<string>> Act_done)
    {
        this.RunPowershellCMD("adb devices",output=>{
            this.Load_list_devices(output,Act_done);
        });
    }

    private void Load_list_devices(string output,UnityAction<List<string>> Act_done){
        string[] lines = output.Split('\n');
        this.deviceList = new();
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (!string.IsNullOrEmpty(line) && line.Contains("device"))
            {
                string[] parts = line.Split('\t');
                if (parts.Length > 0) deviceList.Add(parts[0]);
            }
        }
        Act_done?.Invoke(deviceList);
    }

    private void Load_list_devices(){
        this.Clear_all_item();
        this.ListConnectedDevices((devices)=>{
            this.device_Uis=new();
            foreach(string s in devices){
                Debug.Log(s.ToString());
                GameObject device_item=Instantiate(this.device_item_pfefab);
                device_item.transform.SetParent(this.tr_all_item);
                device_item.transform.localScale=new Vector3(1f,1f,1f);
                Device_ui ui_d= device_item.GetComponent<Device_ui>();
                ui_d.txt_title.text=s;
                ui_d.txt_tip.text="Mobile Device";
                this.device_Uis.Add(ui_d);
            }
        });
    }


    public void Bnt_check_devices(){
        this.Load_list_devices();
    }

    private void Clear_all_item(){
        foreach(Transform tr in this.tr_all_item){
            Destroy(tr.gameObject);
        }
    }

    public void RunPowershellCMD(string command,UnityAction<string> Act_done=null)
    {
        System.Diagnostics.Process process = new();
        process.StartInfo.FileName = "powershell.exe";
        process.StartInfo.Arguments = command;

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        //process.WaitForExit();
        if (string.IsNullOrEmpty(error))
            Debug.Log("Output: " + output);
        else
            Debug.LogError("Error: " + error);
        Act_done?.Invoke(output);
    }

    public void RunADBCommand_All_Device(string s_command){
        for(int i=0;i<this.deviceList.Count;i++) {
            Debug.Log(this.deviceList[i]+" -> "+s_command);
            this.RunPowershellCMD("adb -s "+this.deviceList[i]+" "+s_command);
        }
    }

    public void Run_Duble_tap_all_device(int x, int y,string timer_ms){
        for(int i=0;i<this.deviceList.Count;i++) {
            this.RunPowershellCMD("adb -s "+this.deviceList[i]+" shell input tap "+x+" "+y);
            this.RunPowershellCMD("adb -s "+this.deviceList[i]+" shell sleep "+timer_ms);
            this.RunPowershellCMD("adb -s "+this.deviceList[i]+" shell input tap "+x+" "+y);
        }
        Debug.Log("Run_Duble_tap_all_device");
    }

    public void CallMethodWithDelay(float delayInSeconds, System.Action methodToCall)
    {
        StartCoroutine(ExecuteAfterDelay(delayInSeconds, methodToCall));
    }

    private IEnumerator ExecuteAfterDelay(float delay, System.Action methodToCall)
    {
        yield return new WaitForSeconds(delay);
        methodToCall?.Invoke();
    }

    public void Btn_exit(){
        Application.Quit();
    }

    public void Btn_Reset(){
        for(int i=0;i<this.device_Uis.Count;i++){
            this.device_Uis[i].Reset();
        }
    }

    public void Save_data(){

        this.leng_action=int.Parse(this.inp_action_count.text);
        this.session_count=int.Parse(this.inp_session_count.text);
        this.session_timer=int.Parse(this.inp_session_timer.text);

        PlayerPrefs.SetInt("leng_action",int.Parse(this.inp_action_count.text));
        PlayerPrefs.SetInt("session_count",int.Parse(this.inp_session_count.text));
        PlayerPrefs.SetInt("session_timer",int.Parse(this.inp_session_timer.text));

        this.Check_limit_timer();
    }

    private void Check_limit_timer(){
        int timer_limit=(this.leng_action*int.Parse(this.timer_action.ToString()))+3;
        this.text_txt_limit.text="Thời gian phải lớn hơn > "+timer_limit+" giây";
    }

    public void Btn_kill(){
        this.RunPowershellCMD("adb kill-server");
    }
}
