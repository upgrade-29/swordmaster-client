using System;
using System.Collections.Generic;
using UnityEngine;

// 구매 응답의 유물 최종 스냅샷만 사용자 상태에 반영한다.
public class ShopRewardApplier
{
    private readonly User user;

    public ShopRewardApplier(User user)
    {
        this.user = user ?? throw new ArgumentNullException(nameof(user));
    }

    // null은 서버가 필드를 보내지 않은 상태, 빈 배열은 변경 항목이 없음을 나타내며 둘 다 상태는 바꾸지 않는다.
    public void Apply(IReadOnlyList<ArtifactResponse> changedArtifacts)
    {
        if (changedArtifacts == null)
        {
            return;
        }

        if (changedArtifacts.Count == 0)
        {
            return;
        }

        foreach (ArtifactResponse changedArtifact in changedArtifacts)
        {
            if (changedArtifact == null || string.IsNullOrEmpty(changedArtifact.code))
            {
                Debug.LogWarning("상점 구매 응답의 유물 스냅샷이 올바르지 않아 반영하지 않습니다.");
                continue;
            }

            UserArtifact ownedArtifact = user.GetArtifact(changedArtifact.code);
            if (ownedArtifact == null)
            {
                user.AddArtifact(changedArtifact.ToUserArtifact());
                continue;
            }

            ownedArtifact.SyncFromServerSnapshot(changedArtifact.level,
                changedArtifact.materialCount, changedArtifact.equipSlot);
        }
    }
}
