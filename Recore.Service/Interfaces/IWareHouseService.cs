﻿using Recore.Domain.Configurations;
using Recore.Service.DTOs.BonusSetting;
using Recore.Service.DTOs.WareHouses;

namespace Recore.Service.Interfaces;

public interface IWareHouseService
{
	ValueTask<WareHouseResultDto> AddAsync(BonusSettingCreationDto dto);
	ValueTask<WareHouseResultDto> ModifyAsync(BonusSettingUpdateDto dto);
	ValueTask<bool> RemoveAsync(long id);
	ValueTask<WareHouseResultDto> RetrieveByIdAsync(long id);
	ValueTask<IEnumerable<BonusSettingResultDto>> RetrieveAllAsync(PaginationParams @params);
}