using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Hamster.Addressable
{
    /// <summary>
    /// author : shlifedev
    /// 다운로드 필요 파일이 있는지 (size != 0) 확인 --> 패치 --> 접속 
    /// </summary>
    public class AddressablePatcher : MonoBehaviour
    {
        public enum EPatchAccept
        {
            Wait, Accept, Refuse 
        }
        public EPatchAccept waitUntilAcceeptPatch = EPatchAccept.Wait; 
        public static AddressablePatcher Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = FindObjectOfType<AddressablePatcher>();                    
                }
                return inst;
            }
        }
        private static AddressablePatcher inst; 
        private void Awake()
        {
            StartPatch(
                (x,x2) =>
                {
                    if (x)
                    {
                        
                    }
                    else
                    {
                        
                    }
                }, 
                () => { },  
                (x) => { },
                () => { }, 
                () => { });
        } 
        /// <summary>
        /// 각 콜백에 UI 구현을 연동
        /// onRequireDownload는 true/false로 RequireDownload의 필요 여부를 알려줍니다.
        /// </summary> 
        public void StartPatch(System.Action<bool, long> onRequireDownload, System.Action onFailedDownloadSizeCheck, System.Action<float> onUpdateDownloaded, System.Action onCompleteDownload, System.Action onFailedDownload)
        {
            StartCoroutine(EStartPatch(onRequireDownload, onFailedDownloadSizeCheck, onUpdateDownloaded, onCompleteDownload, onFailedDownload));
        }

        
        /// <summary>
        /// 쉬운 코드 플로우로 구현하기 위해 해당 코루틴 함수 안에서 패치 과정이 이루어 집니다.
        /// 패치 시스템 구현자는 해당 코루틴 함수구문만 분석 한뒤 각 콜백에 UI 변경 사항등을 구현하시면 됩니다.
        /// </summary> 
        IEnumerator EStartPatch(System.Action<bool, long> onRequireDownload, System.Action onFailedDownloadSizeCheck, System.Action<float> onUpdateDownloaded, System.Action onCompleteDownload, System.Action onFailedDownload)
        {
            long downloadSize = 0L;
            int frame = 0;
            var downloadSizeTask = Addressables.GetDownloadSizeAsync("default");
            
            //다운로드 사이즈의 Task의 완료를 기다립니다.
            yield return new WaitUntil(() => downloadSizeTask.IsDone);
            if (downloadSizeTask.Status == AsyncOperationStatus.Failed)
            {
                Debug.Log("[AddressablePatcher] => failed download size check.");
                onFailedDownloadSizeCheck?.Invoke(); 
                goto BreakCoroutine; 
            } 
            else if (downloadSizeTask.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"[AddressablePatcher] => Require Download Size : {downloadSizeTask.Result}");
                downloadSize = downloadSizeTask.Result;
                //다운로드를 해야하는 상황
                if (downloadSize != 0)
                { 
                    Debug.Log("[AddressablePatcher] => Need Downlaod! Start Download Now..");
                    //다운로드 해야할 파일이 있음을 알립니다.
                    onRequireDownload?.Invoke(true, downloadSize);
                    //waitUntilAcceeptPatch가 True가 될때까지 기다립니다.  
                    
                    Debug.Log("[AddressablePatcher] => Wait User waitUntilAcceeptPatch boolean..");
                    yield return new WaitUntil(() => waitUntilAcceeptPatch != EPatchAccept.Wait);
                    if (waitUntilAcceeptPatch == EPatchAccept.Accept)
                    {
                        //다운로드를 시작합니다. autoReleaseHandle의 값이 true면 자동으로 다운로드 후 메모리에서 해제합니다(내부 구현 확인하였음)
                        var downloadTask = Addressables.DownloadDependenciesAsync("default", true);

                        //다운로드 콜백추가
                        downloadTask.Completed += (x) =>
                        {
                            Debug.Log("[AddressablePatcher] => Download Complete!");
                            onUpdateDownloaded?.Invoke(downloadTask.PercentComplete);
                            onCompleteDownload?.Invoke();
                        };

                        //다운로드 중인 상태.
                        while (downloadTask.IsDone == false)
                        {
                            onUpdateDownloaded?.Invoke(downloadTask.PercentComplete);
                            yield return null;
                        }
                    }

                    if (waitUntilAcceeptPatch == EPatchAccept.Refuse)
                    {
                        
                    }
                } 
                //다운로드 할 필요가 없음 
                else
                {
                    Debug.Log("[AddressablePatcher] => Not need Download. Patch Complete.");
                    onRequireDownload?.Invoke(false, 0);
                    onCompleteDownload?.Invoke();
                    yield return null;
                }
            } 
            
            //코루틴을 강제 종료하는 라인.
            BreakCoroutine:
            yield return null;
        } 
 
        public bool ValidateFiles()
        {
            return false;
        } 
    }
}
